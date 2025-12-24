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

        if (command == "summarize" || command == "sumirize")
        {
            Tools tool = new SummarizeTool();
            await tool.Ejecutar(args);
        }
        else if (command == "fix")
        {
            Tools tool = new FixTool();
            await tool.Ejecutar(args);
        }
        else if (command == "implement" || command == "add")
        {
            Tools tool = new ImplementTool();
            await tool.Ejecutar(args);
        }
        else if (command == "help")
        {
            ShowHelp();
        }
        else
        {
            Print($"Comando '{command}' no reconocido.", ConsoleColor.Red);
        }
    }

    static void ShowHelp()
    {
        Print("Uso: dbt <comando> [argumentos]", ConsoleColor.Yellow);
        Print("Comandos disponibles:", ConsoleColor.Cyan);
        Console.WriteLine("  summarize <ruta>   Analiza y resume un archivo o directorio.");
        Console.WriteLine("  fix <ruta>         Ayuda a corregir errores en un archivo.");
        Console.WriteLine("  implement <origen> <destino> Implementa requerimientos o código en un proyecto.");
        Console.WriteLine("  help               Muestra esta ayuda.");
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
