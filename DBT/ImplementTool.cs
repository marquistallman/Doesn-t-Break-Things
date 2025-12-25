using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DBT;

public class ImplementTool : Tools
{
    public override async Task Ejecutar(string[] args)
    {
        if (args.Length < 3)
        {
            Program.Print("Uso: dbt implement <origen> <destino>", ConsoleColor.Yellow);
            Console.WriteLine("  <origen>: Archivo de requisitos (ej. requirements.txt) o carpeta con código fuente.");
            Console.WriteLine("  <destino>: Carpeta del proyecto donde se realizará la implementación.");
            return;
        }

        string sourcePath = args[1];
        string targetPath = args[2];

        // Validar origen
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            Program.Print($"Error: La ruta de origen '{sourcePath}' no existe.", ConsoleColor.Red);
            return;
        }

        // Validar destino (debe ser un directorio de proyecto existente)
        if (!Directory.Exists(targetPath))
        {
            Program.Print($"Error: La ruta de destino '{targetPath}' no existe o no es un directorio.", ConsoleColor.Red);
            return;
        }

        Program.Print("=== Herramienta de Implementación ===", ConsoleColor.Magenta);
        Program.Print($"Origen: {sourcePath}", ConsoleColor.Gray);
        Program.Print($"Destino: {targetPath}", ConsoleColor.Gray);

        try
        {
            // 1. Leer contexto del origen (Requisitos o Código)
            Program.Print("Leyendo contexto de origen...", ConsoleColor.Cyan);
            string sourceContext = await GetContext(sourcePath);

            // 2. Leer estructura del destino
            Program.Print("Analizando proyecto destino...", ConsoleColor.Cyan);
            string targetContext = await GetContext(targetPath);

            // 3. Preparar Payload
            var payload = new
            {
                SourcePath = sourcePath,
                SourceContext = sourceContext,
                TargetContext = targetContext
            };
            
            string jsonPayload = JsonSerializer.Serialize(payload);

            // 4. Generar Plan de Implementación (Lista de archivos e instrucciones)
            OllamaImplementPlan planner = new OllamaImplementPlan();
            await planner.SetModel();
            List<FilePlanItem>? plan = null;
            int maxRetries = 3;
            int currentRetry = 0;

            while (plan == null && currentRetry < maxRetries)
            {
                currentRetry++;
                if (currentRetry > 1) Program.Print($"Reintentando generación del plan (Intento {currentRetry}/{maxRetries})...", ConsoleColor.Yellow);

                string planJson = await planner.Ejecutar(jsonPayload);

                // Guardar el plan JSON para revisión
                string? planDir = File.Exists(sourcePath) ? Path.GetDirectoryName(sourcePath) : sourcePath;
                string planFile = string.IsNullOrEmpty(planDir) ? "implementation_plan.json" : Path.Combine(planDir, "implementation_plan.json");
                await File.WriteAllTextAsync(planFile, planJson);

                // Limpieza de JSON: Intentar extraer el array o el objeto del texto si el modelo incluyó explicaciones
                string processedJson = planJson;
                int idxStartArr = planJson.IndexOf('[');
                int idxEndArr = planJson.LastIndexOf(']');
                
                if (idxStartArr >= 0 && idxEndArr > idxStartArr)
                {
                    processedJson = planJson.Substring(idxStartArr, idxEndArr - idxStartArr + 1);
                }
                else
                {
                    // Si no encuentra array, intentar buscar objeto único para el fallback
                    int idxStartObj = planJson.IndexOf('{');
                    int idxEndObj = planJson.LastIndexOf('}');
                    if (idxStartObj >= 0 && idxEndObj > idxStartObj)
                    {
                        processedJson = planJson.Substring(idxStartObj, idxEndObj - idxStartObj + 1);
                    }
                }

                // 5. Procesar Plan
                try 
                {
                    plan = JsonSerializer.Deserialize<List<FilePlanItem>>(processedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch 
                {
                    // Intento de recuperación: si devuelve un objeto único en vez de array (tu caso específico)
                    try {
                        var single = JsonSerializer.Deserialize<FilePlanItem>(processedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (single != null) plan = new List<FilePlanItem> { single };
                    } catch { }

                    // Intento de recuperación 2: Diccionario { "path": { "instruction": "..." } } (Formato creativo del modelo)
                    if (plan == null)
                    {
                        try 
                        {
                            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(processedJson);
                            if (dict != null)
                            {
                                plan = new List<FilePlanItem>();
                                foreach (var kvp in dict)
                                {
                                    string instr = "";
                                    // Manejar si el valor es un objeto o un string directo
                                    if (kvp.Value.ValueKind == JsonValueKind.Object)
                                    {
                                        if (kvp.Value.TryGetProperty("instruction", out var i) || kvp.Value.TryGetProperty("instructions", out i))
                                            instr = i.GetString() ?? "";
                                    }
                                    else if (kvp.Value.ValueKind == JsonValueKind.String)
                                        instr = kvp.Value.GetString() ?? "";
                                    
                                    plan.Add(new FilePlanItem { Name = kvp.Key, Instructions = instr });
                                }
                            }
                        } catch { }
                    }

                    if (plan == null) Program.Print($"Error al interpretar JSON (Intento {currentRetry}).", ConsoleColor.Red);
                    
                    if (currentRetry == maxRetries && plan == null) Console.WriteLine(planJson);
                }
            }

            if (plan == null || plan.Count == 0)
            {
                Program.Print("El plan de implementación está vacío.", ConsoleColor.Yellow);
                return;
            }

            Program.Print($"\nPlan generado: {plan.Count} archivos a procesar.", ConsoleColor.Green);

            // 6. Ejecutar Plan (Archivo por archivo)
            OllamaImplementFile generator = new OllamaImplementFile();
            await generator.SetModel(); // Usar el mismo modelo configurado
            
            foreach (var item in plan)
            {
                // Validar que la ruta sea válida para evitar errores de acceso (ej: ruta vacía apunta al directorio raíz)
                if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim() == "." || item.Name.Trim() == "/" || item.Name.Trim() == "\\")
                {
                    Program.Print("Advertencia: Se omitió un archivo del plan por tener una ruta inválida o vacía.", ConsoleColor.Yellow);
                    continue;
                }

                Program.Print($"\nGenerando: {item.Name}", ConsoleColor.Cyan);
                try 
                {
                    string content = await generator.GenerarArchivo(item.Name, item.Instructions, sourceContext, targetContext);
                    await SaveFile(targetPath, item.Name, content);
                }
                catch (Exception ex)
                {
                    Program.Print($"Error generando {item.Name}: {ex.Message}", ConsoleColor.Red);
                }
            }
        }
        catch (Exception ex)
        {
            Program.Print($"Error durante la ejecución: {ex.Message}", ConsoleColor.Red);
        }
    }

    // Clase auxiliar para deserializar el plan
    private class FilePlanItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("instructions")]
        public string Instructions { get; set; } = "";
    }

    // Lee un archivo o recorre un directorio recursivamente para obtener todo el código
    private async Task<string> GetContext(string path)
    {
        if (File.Exists(path)) return await File.ReadAllTextAsync(path);
        
        if (Directory.Exists(path))
        {
            var sb = new StringBuilder();
            // Obtener archivos excluyendo carpetas basura comunes para no saturar el contexto
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                // Filtros de exclusión de directorios
                if (file.Contains(Path.DirectorySeparatorChar + ".git") || 
                    file.Contains(Path.DirectorySeparatorChar + "bin") || 
                    file.Contains(Path.DirectorySeparatorChar + "obj") || 
                    file.Contains(Path.DirectorySeparatorChar + "node_modules") ||
                    file.Contains(Path.DirectorySeparatorChar + ".vs")) 
                    continue;

                // Ignorar archivos binarios o desconocidos para no saturar el contexto
                // Ahora SourceFile soporta más tipos, pero mantenemos el filtro de seguridad
                if (SourceFile.IdentificarLenguaje(file) == "Desconocido" && !file.EndsWith(".txt")) continue;
                
                try 
                {
                    string relPath = Path.GetRelativePath(path, file);
                    sb.AppendLine($"// --- FILE: {relPath} ---");
                    sb.AppendLine(await File.ReadAllTextAsync(file));
                    sb.AppendLine();
                }
                catch (Exception ex)
                {
                    // Ignorar archivos que no se pueden leer (bloqueados o sin permisos)
                    Console.WriteLine($"[Warning] No se pudo leer {file}: {ex.Message}");
                }
            }
            return sb.ToString();
        }
        return "";
    }

    private async Task SaveFile(string root, string relativePath, string content)
    {
        string fullPath = Path.Combine(root, relativePath);
        string? dir = Path.GetDirectoryName(fullPath);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        
        await File.WriteAllTextAsync(fullPath, content);
        Program.Print($"Archivo generado: {relativePath}", ConsoleColor.Green);
    }
}
