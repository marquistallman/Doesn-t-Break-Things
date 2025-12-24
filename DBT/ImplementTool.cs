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

            // 4. Llamar a IA
            OllamaImplement ollama = new OllamaImplement();
            await ollama.SetModel();
            
            string rawResponse = await ollama.Ejecutar(jsonPayload);
            
            // 5. Procesar respuesta (Extraer texto limpio)
            OllamaResponse responseProcessor = new OllamaResponse();
            string planCode = await responseProcessor.Ejecutar(rawResponse);

            // 6. Aplicar cambios (Escribir archivos)
            await ApplyImplementation(targetPath, planCode);
        }
        catch (Exception ex)
        {
            Program.Print($"Error durante la ejecución: {ex.Message}", ConsoleColor.Red);
        }
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

    // Parsea la respuesta de la IA buscando marcadores "// FILE:" para crear/sobrescribir archivos
    private async Task ApplyImplementation(string targetRoot, string aiResponse)
    {
        Program.Print("\n--- Aplicando Implementación ---", ConsoleColor.Magenta);
        
        using StringReader sr = new StringReader(aiResponse);
        string? line;
        string? currentFile = null;
        StringBuilder fileContent = new StringBuilder();

        while ((line = await sr.ReadLineAsync()) != null)
        {
            string trimmed = line.Trim();
            
            // Detectar marcador de archivo (Soporta // FILE: y # FILE:)
            if (trimmed.StartsWith("// FILE:") || trimmed.StartsWith("# FILE:"))
            {
                // Guardar archivo anterior si existe
                if (currentFile != null) await SaveFile(targetRoot, currentFile, fileContent.ToString());

                // Iniciar nuevo archivo
                currentFile = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                fileContent.Clear();
                continue;
            }

            // Ignorar delimitadores de markdown si están solos
            if (trimmed.StartsWith("```")) continue;

            if (currentFile != null) fileContent.AppendLine(line);
        }

        if (currentFile != null) await SaveFile(targetRoot, currentFile, fileContent.ToString());
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
