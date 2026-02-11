using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using DBT.Services;
namespace DBT.Tools;
public class CreateTool : Tools
{
    public override async Task Ejecutar(string[] args)
    {
        string ideaContent = "";
        string targetPath = "";

        // Modo Automático: Se proporcionan argumentos
        if (args.Length >= 3)
        {
            string ideaPath = args[1];
            targetPath = args[2];

            if (!File.Exists(ideaPath))
            {
                Program.Print($"Error: El archivo de idea '{ideaPath}' no existe.", ConsoleColor.Red);
                return;
            }
            ideaContent = await File.ReadAllTextAsync(ideaPath);

            Program.Print("=== Herramienta de Creación ===", ConsoleColor.Magenta);
            Program.Print($"Idea: {ideaPath}", ConsoleColor.Gray);
            Program.Print($"Destino: {targetPath}", ConsoleColor.Gray);
        }
        // Modo Interactivo: Sin argumentos o argumentos incompletos
        else if (args.Length == 1)
        {
            Program.Print("=== Modo Interactivo de Creación ===", ConsoleColor.Magenta);
            
            // 1. Capturar Idea
            Console.WriteLine("\nDescribe tu idea para el proyecto.");
            Console.WriteLine("Escribe línea por línea. Escribe 'FIN' en una línea nueva para terminar:");
            
            var lines = new List<string>();
            while (true)
            {
                Console.Write("> ");
                string? line = Console.ReadLine();
                if (line?.Trim().ToUpper() == "FIN") break;
                if (line != null) lines.Add(line);
            }
            
            ideaContent = string.Join(Environment.NewLine, lines);
            if (string.IsNullOrWhiteSpace(ideaContent))
            {
                Program.Print("No se ingresó ninguna idea.", ConsoleColor.Red);
                return;
            }

            // Opción de guardar la idea
            Console.WriteLine("\n¿Deseas guardar esta idea en un archivo? (s/n)");
            if (Console.ReadLine()?.Trim().ToLower() == "s")
            {
                Console.Write("Nombre del archivo (ej. idea.txt): ");
                string? savePath = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(savePath))
                {
                    await File.WriteAllTextAsync(savePath, ideaContent);
                    Program.Print($"Idea guardada en {savePath}", ConsoleColor.Green);
                }
            }

            // 2. Seleccionar Destino
            Program.Print("\nSelecciona la carpeta de destino:", ConsoleColor.Cyan);
            targetPath = SelectDirectoryInteractive();
            Program.Print($"Destino seleccionado: {targetPath}", ConsoleColor.Green);
        }
        else
        {
            Program.Print("Uso: dbt create <idea> <destino>  O  dbt create (para modo interactivo)", ConsoleColor.Yellow);
            return;
        }

        try
        {
            Program.Print("Analizando idea y generando plan de proyecto...", ConsoleColor.Cyan);
            
            OllamaCreate ollama = new OllamaCreate();
            await ollama.SetModel();
            string requirementsContent = await ollama.Ejecutar(ideaContent);

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
                Program.Print($"Directorio creado: {targetPath}", ConsoleColor.Green);
            }

            string reqFilePath = Path.Combine(targetPath, "requirements.txt");
            await File.WriteAllTextAsync(reqFilePath, requirementsContent);
            
            Program.Print($"Plan guardado en: {reqFilePath}", ConsoleColor.Green);

            Program.Print("\n¿Deseas proceder con la implementación ahora? (s/n)", ConsoleColor.Yellow);
            string? response = Console.ReadLine();

            if (response?.Trim().ToLower() == "s")
            {
                Program.Print("Iniciando implementación...", ConsoleColor.Cyan);
                string[] implementArgs = new string[] { "implement", reqFilePath, targetPath };
                Tools implementTool = new ImplementTool();
                await implementTool.Ejecutar(implementArgs);
            }
        }
        catch (Exception ex)
        {
            Program.Print($"Error: {ex.Message}", ConsoleColor.Red);
        }
    }

    private string SelectDirectoryInteractive()
    {
        string currentPath = Directory.GetCurrentDirectory();
        
        while (true)
        {
            Console.WriteLine($"\n--- Explorador: {currentPath} ---");
            Console.WriteLine(" [0] .  (Seleccionar actual)");
            Console.WriteLine(" [1] .. (Subir nivel)");
            
            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(currentPath);
            }
            catch
            {
                Console.WriteLine("Error: No se puede acceder a este directorio.");
                var parent = Directory.GetParent(currentPath);
                if (parent != null) currentPath = parent.FullName;
                continue;
            }

            int optionIndex = 2;
            var dirList = new List<string>();

            // Mostrar subdirectorios (máximo 15 para no saturar)
            foreach (var dir in dirs.Take(50))
            {
                Console.WriteLine($" [{optionIndex}] /{Path.GetFileName(dir)}");
                dirList.Add(dir);
                optionIndex++;
            }
            
            Console.WriteLine($" [{optionIndex}] + (Crear nueva carpeta)");
            Console.Write("Opción: ");
            
            string? input = Console.ReadLine()?.Trim();

            if (input == "0" || input == ".") return currentPath;
            
            if (input == "1" || input == "..")
            {
                var parent = Directory.GetParent(currentPath);
                if (parent != null) currentPath = parent.FullName;
                continue;
            }

            if (input == "+" || input == optionIndex.ToString())
            {
                Console.Write("Nombre de nueva carpeta: ");
                string? newName = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    string newPath = Path.Combine(currentPath, newName);
                    if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
                    currentPath = newPath;
                }
                continue;
            }

            if (int.TryParse(input, out int selection) && selection >= 2 && selection < optionIndex)
            {
                currentPath = dirList[selection - 2];
            }
        }
    }
}