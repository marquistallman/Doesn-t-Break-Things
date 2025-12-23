// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;

namespace DBT;
class Program
{
    static async Task Main(string[] args)
    {
        // Habilitar caracteres especiales y asegurar codificación
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0)
        {
            Print("Uso: dbt <comando> [argumentos]", ConsoleColor.Yellow);
            Print("Comandos disponibles:", ConsoleColor.Cyan);
            Console.WriteLine("  summarize <ruta>   Analiza y resume un archivo o directorio.");
            return;
        }

        string command = args[0].ToLower();

        if (command == "summarize" || command == "sumirize")
        {
            if (args.Length < 2)
            {
                Print("Error: Debes especificar la ruta del archivo o directorio.", ConsoleColor.Red);
                return;
            }

            string filePath = args[1];
            OllamaInput ollama = new OllamaInput();
            await ollama.SetModel();

            try
            {
                if (File.Exists(filePath))
                {
                    await showFileData(filePath, ollama);
                }
                else if (Directory.Exists(filePath))
                {
                    await saveFileData(filePath, ollama);
                }
                else
                {
                    Print("La ruta especificada no existe.", ConsoleColor.Red);
                }
            }
            catch (Exception ex)
            {
                Print($"Error: {ex.Message}", ConsoleColor.Red);
            }
        }
        else
        {
            Print($"Comando '{command}' no reconocido.", ConsoleColor.Red);
        }
    }
    public static async Task showFileData(string filePath, OllamaInput ollama)
    {
        SourceFile archivo = new SourceFile(filePath);
        Print($"\nArchivo cargado: {Path.GetFileName(filePath)}", ConsoleColor.Cyan);
        Console.WriteLine($"Líneas: {archivo.Lineas.Count} | Lenguaje: {archivo.Lenguaje}");

            Resume resume = new Resume();
            resume.Archivo = archivo.Ruta;
            resume.Analizar(archivo.Lineas);
            Print("\n--- Resumen del Análisis ---", ConsoleColor.Green);
            Console.WriteLine($"Bucles detectados: {resume.Bucles}");
            Console.WriteLine($"Declaraciones de variables: {resume.Declaraciones}");
            Console.WriteLine($"Transformaciones de variables: {resume.Transformaciones}");
            Console.WriteLine($"Clases usadas: {string.Join(", ", resume.ClasesUsadas)}");
            Console.WriteLine($"Métodos usados: {string.Join(", ", resume.MetodosUsados)}");
            Print("----------------------------", ConsoleColor.Green);

            string jsonAnalysis = JsonSerializer.Serialize(resume, new JsonSerializerOptions { WriteIndented = true });
            string rawResponse = await ollama.Ejecutar(jsonAnalysis);
            
            OllamaResponse responseProcessor = new OllamaResponse();
            string finalReport = await responseProcessor.Ejecutar(rawResponse);

            Print("\n--- Reporte de IA ---", ConsoleColor.Magenta);
            Console.WriteLine(finalReport);
            Print("---------------------", ConsoleColor.Magenta);

            await File.WriteAllTextAsync("summarize.txt", finalReport);
            Print("\nResumen guardado en summarize.txt", ConsoleColor.Yellow);
    }
    public static async Task saveFileData(string directoryPath, OllamaInput ollama)
    {
        List<Resume> resumes = new List<Resume>();
        Print($"\nAnalizando directorio: {directoryPath}", ConsoleColor.Cyan);
        foreach (var filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            // Ignorar archivos cuyo lenguaje no sea reconocido
            if (SourceFile.IdentificarLenguaje(filePath) == "Desconocido") continue;

            try
            {
                SourceFile archivo = new SourceFile(filePath);
                Resume resume = new Resume();
                resume.Archivo = archivo.Ruta;
                resume.Analizar(archivo.Lineas);
                resumes.Add(resume);
                Console.WriteLine($" - Analizado: {Path.GetFileName(filePath)} ({archivo.Lenguaje})");
            }
            catch (IOException ex)
            {
                Print($"Error al leer el archivo {filePath}: {ex.Message}", ConsoleColor.Red);
            }
        }

        string salida = JsonSerializer.Serialize(resumes, new JsonSerializerOptions { WriteIndented = true });
        string outputFilePath = "analysis_results.json";
        File.WriteAllText(outputFilePath, salida);
        Print($"\nMetadatos guardados en: {Path.GetFileName(outputFilePath)}", ConsoleColor.Green);

        Print("Generando reporte global con IA...", ConsoleColor.Yellow);
        string rawResponse = await ollama.Ejecutar(salida);
        
        OllamaResponse responseProcessor = new OllamaResponse();
        string finalReport = await responseProcessor.Ejecutar(rawResponse);

        Print("\n--- Reporte de IA (Global) ---", ConsoleColor.Magenta);
        Console.WriteLine(finalReport);
        Print("------------------------------", ConsoleColor.Magenta);

        await File.WriteAllTextAsync("summarize.txt", finalReport);
        Print("\nResumen global guardado en summarize.txt", ConsoleColor.Yellow);
    }

    static void Print(string message, ConsoleColor color)
    {
        // Si la salida se redirige a un archivo, no usar colores
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(message);
            return;
        }

        // Usar códigos ANSI para mayor compatibilidad en VS Code y Terminales modernas
        string ansi = color switch
        {
            ConsoleColor.Red => "\u001b[31m",
            ConsoleColor.Green => "\u001b[32m",
            ConsoleColor.Yellow => "\u001b[33m",
            ConsoleColor.Cyan => "\u001b[36m",
            ConsoleColor.Magenta => "\u001b[35m",
            _ => "\u001b[37m" // Blanco por defecto
        };
        Console.WriteLine($"{ansi}{message}\u001b[0m");
    }
}
