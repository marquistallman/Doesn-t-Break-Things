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
            ShowHelp();
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "summarize":
            case "sumirize":
                await new SummarizeTool().Ejecutar(args);
                break;
            case "fix":
                await new FixTool().Ejecutar(args);
                break;
            case "implement":
            case "add":
                await new ImplementTool().Ejecutar(args);
                break;
            case "create":
                await new CreateTool().Ejecutar(args);
                break;
            case "requirements":
            case "specs":
                ShowRequirements();
                break;
            case "help":
                ShowHelp();
                break;
            default:
                Print($"Comando '{command}' no reconocido.", ConsoleColor.Red);
                break;
        }
    }

    static void ShowHelp()
    {
        Print("Uso: dbt <comando> [argumentos]", ConsoleColor.Yellow);
        Print("Comandos disponibles:", ConsoleColor.Cyan);
        Console.WriteLine("  summarize <ruta>   Analiza y resume un archivo o directorio.");
        Console.WriteLine("  fix <ruta>         Ayuda a corregir errores en un archivo.");
        Console.WriteLine("  implement <origen> <destino> Implementa requerimientos o código en un proyecto.");
        Console.WriteLine("  create <idea> <destino> Crea la estructura y requisitos de un proyecto desde una idea.");
        Console.WriteLine("  requirements       Muestra los requisitos y capacidades de esta aplicación.");
        Console.WriteLine("  help               Muestra esta ayuda.");
    }

    static void ShowRequirements()
    {
        Print("=== Requisitos y Capacidades de DBT ===", ConsoleColor.Magenta);
        Console.WriteLine("1. Análisis de Código (Summarize): Resumen de archivos y directorios.");
        Console.WriteLine("2. Corrección (Fix): Sugerencias de arreglos para errores de código.");
        Console.WriteLine("3. Implementación (Implement): Generación de código basada en planes y requisitos.");
        Console.WriteLine("4. Creación (Create): Generación de estructura de proyecto desde ideas.");
        Console.WriteLine("5. Core: Integración con Ollama, manejo de contexto y configuración dinámica.");
    }

    public static void Print(string message, ConsoleColor color)
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
