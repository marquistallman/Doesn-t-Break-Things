using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class FixTool : Tools
{
    public override async Task Ejecutar(string[] args)
    {
        if (args.Length < 2)
        {
            Print("Uso: dbt fix <archivo>", ConsoleColor.Yellow);
            return;
        }

        string filePath = args[1];
        if (!File.Exists(filePath))
        {
            Print($"El archivo {filePath} no existe.", ConsoleColor.Red);
            return;
        }

        SourceFile archivo = new SourceFile(filePath);
        Problems problems = new Problems();

        Print($"\n--- Asistente de Corrección (Fix) ---", ConsoleColor.Cyan);
        Print($"Archivo: {Path.GetFileName(filePath)}", ConsoleColor.White);

        // 1. Definir el problema (Interacción con el usuario)
        Console.WriteLine("\nDescribe el error o problema (Enter para análisis general):");
        string descripcion = Console.ReadLine()?.Trim() ?? "";
        
        int linea = 0;
        if (!string.IsNullOrEmpty(descripcion))
        {
            Console.WriteLine("¿Línea aproximada del error? (0 para todo el archivo):");
            int.TryParse(Console.ReadLine(), out linea);
        }

        // Cargar datos en la clase Problems
        var listaErrores = new List<(string, int)>();
        if (!string.IsNullOrEmpty(descripcion))
        {
            listaErrores.Add((descripcion, linea));
        }
        problems.CargarProblemas(archivo, listaErrores);

        // 2. Determinar contexto del código (Slicing)
        string codigoAEnviar;
        string rangoInfo;
        int inicio = 0;
        int cantidadLineas = archivo.Lineas.Count;

        if (linea > 0 && linea <= archivo.Lineas.Count)
        {
            int radio = 20; // Líneas de contexto arriba y abajo
            inicio = Math.Max(0, linea - 1 - radio);
            int fin = Math.Min(archivo.Lineas.Count, linea + radio);
            cantidadLineas = fin - inicio;
            
            var fragmento = archivo.Lineas.GetRange(inicio, cantidadLineas);
            codigoAEnviar = string.Join(Environment.NewLine, fragmento);
            rangoInfo = $"Líneas {inicio + 1} a {fin}";
            Print($"\nContexto extraído: {rangoInfo}", ConsoleColor.Gray);
        }
        else
        {
            codigoAEnviar = string.Join(Environment.NewLine, archivo.Lineas);
            rangoInfo = "Archivo completo";
            Print("\nContexto seleccionado: Archivo completo", ConsoleColor.Gray);
        }

        // 3. Contexto de otros archivos (Dependencias)
        string contextoExtra = "";
        Console.WriteLine("\n¿El error depende de otros archivos o clases? (s/n):");
        if (Console.ReadLine()?.Trim().ToLower() == "s")
        {
            Console.WriteLine("Introduce la ruta de la carpeta o archivo relacionado:");
            string rutaExtra = Console.ReadLine()?.Trim() ?? "";
            
            if (File.Exists(rutaExtra))
            {
                contextoExtra += $"\n// --- Referencia Externa: {Path.GetFileName(rutaExtra)} ---\n{File.ReadAllText(rutaExtra)}\n";
                Print($"Añadido archivo: {Path.GetFileName(rutaExtra)}", ConsoleColor.Green);
            }
            else if (Directory.Exists(rutaExtra))
            {
                var archivosExtra = Directory.GetFiles(rutaExtra, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => SourceFile.IdentificarLenguaje(f) != "Desconocido" && Path.GetFullPath(f) != Path.GetFullPath(filePath))
                    .Take(5); // Límite de seguridad

                foreach (var f in archivosExtra)
                {
                    contextoExtra += $"\n// --- Referencia Externa: {Path.GetFileName(f)} ---\n{File.ReadAllText(f)}\n";
                }
                Print($"Añadidos {archivosExtra.Count()} archivos de referencia.", ConsoleColor.Green);
            }
        }

        // 4. Preparar Payload y Enviar
        var payload = new
        {
            Archivo = Path.GetFileName(filePath),
            Lenguaje = archivo.Lenguaje,
            Problemas = problems.ListaProblemas, // Usamos la clase Problems
            Ubicacion = rangoInfo,
            CodigoPrincipal = codigoAEnviar,
            ContextoAdicional = contextoExtra
        };

        string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        OllamaFix ollamaFix = new OllamaFix();
        await ollamaFix.SetModel();
        
        string rawResponse = await ollamaFix.Ejecutar(jsonPayload);
        
        OllamaResponse responseProcessor = new OllamaResponse();
        string solucion = await responseProcessor.Ejecutar(rawResponse);

        Print("\n--- Procesando Sugerencias ---", ConsoleColor.Magenta);
        
        // Procesar respuesta para obtener líneas limpias
        List<string> lineasCorregidas = ProcesarRespuesta(solucion);

        if (lineasCorregidas.Count == 0)
        {
            Print("Error: No se pudo extraer código válido de la respuesta de la IA.", ConsoleColor.Red);
            return;
        }

        // Lógica de Reemplazo Directo:
        // Confiamos en que el modelo devuelve el bloque corregido completo.
        // Eliminamos el bloque original y colocamos el nuevo.
        archivo.Lineas.RemoveRange(inicio, cantidadLineas);
        archivo.Lineas.InsertRange(inicio, lineasCorregidas);

        Print($"Se ha aplicado la corrección reemplazando {cantidadLineas} líneas originales por {lineasCorregidas.Count} nuevas.", ConsoleColor.Green);

        // Guardar archivo modificado
        try
        {
            await File.WriteAllLinesAsync(filePath, archivo.Lineas);
            Print($"Archivo actualizado exitosamente: {filePath}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            Print($"Error al guardar el archivo: {ex.Message}", ConsoleColor.Red);
        }
    }

    private List<string> ProcesarRespuesta(string respuesta)
    {
        var lineas = new List<string>();
        
        // Detectar si hay bloques de código reales (al inicio de la línea)
        bool tieneBloqueCodigo = false;
        using (StringReader sr = new StringReader(respuesta))
        {
            string? l;
            while ((l = sr.ReadLine()) != null)
            {
                if (l.Trim().StartsWith("```")) { tieneBloqueCodigo = true; break; }
            }
        }

        bool dentroBloque = false;

        using (StringReader reader = new StringReader(respuesta))
        {
            string? linea;
            while ((linea = reader.ReadLine()) != null)
            {
                string lTrim = linea.Trim();
                if (lTrim.StartsWith("```"))
                {
                    dentroBloque = !dentroBloque;
                    
                    // Si hay código en la misma línea después de los backticks (ej: ```csharp int x=0;)
                    // intentamos recuperarlo si estamos entrando al bloque
                    if (dentroBloque && lTrim.Length > 3)
                    {
                        // Es arriesgado parsear esto, mejor confiamos en que las siguientes líneas tengan el código.
                        // Pero si es una sola línea, el prompt actualizado debería evitarlo.
                    }
                    continue; 
                }

                if (tieneBloqueCodigo)
                {
                    if (dentroBloque) lineas.Add(linea);
                }
                else
                {
                    // Si no hay markdown, somos más permisivos para no romper el formato
                    
                    // Filtros de conversación básicos
                    if (lTrim.StartsWith("Here is") || lTrim.StartsWith("Sure") || lTrim.StartsWith("The code") || lTrim.StartsWith("Note:")) continue;
                    if (lTrim.StartsWith("```")) continue; // Backticks sueltos

                    // Preservar líneas vacías (importante para la legibilidad)
                    // Solo saltamos si es el inicio/fin del archivo (se limpia después)
                    lineas.Add(linea);
                }
            }
        }
        
        // Limpieza vertical (trim) del bloque resultante
        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[0])) lineas.RemoveAt(0);
        while (lineas.Count > 0 && string.IsNullOrWhiteSpace(lineas[lineas.Count - 1])) lineas.RemoveAt(lineas.Count - 1);

        return lineas;
    }
}
