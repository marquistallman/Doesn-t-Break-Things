using System;
using System.IO;
using System.Threading.Tasks;

namespace DBT;

public class CreateTool : Tools
{
    public override async Task Ejecutar(string[] args)
    {
        if (args.Length < 3)
        {
            Program.Print("Uso: dbt create <idea> <destino>", ConsoleColor.Yellow);
            Console.WriteLine("  <idea>: Archivo de texto con la descripción de la idea.");
            Console.WriteLine("  <destino>: Carpeta donde se creará el proyecto.");
            return;
        }

        string ideaPath = args[1];
        string targetPath = args[2];

        if (!File.Exists(ideaPath))
        {
            Program.Print($"Error: El archivo de idea '{ideaPath}' no existe.", ConsoleColor.Red);
            return;
        }

        Program.Print("=== Herramienta de Creación ===", ConsoleColor.Magenta);
        Program.Print($"Idea: {ideaPath}", ConsoleColor.Gray);
        Program.Print($"Destino: {targetPath}", ConsoleColor.Gray);

        try
        {
            string ideaContent = await File.ReadAllTextAsync(ideaPath);

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
}