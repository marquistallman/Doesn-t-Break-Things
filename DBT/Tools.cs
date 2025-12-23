using System;
using System.Threading.Tasks;

public abstract class Tools
{
    public abstract Task Ejecutar(string[] args);

    protected void Print(string message, ConsoleColor color)
    {
        // Si la salida se redirige a un archivo, no usar colores
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(message);
            return;
        }

        // Usar códigos ANSI para mayor compatibilidad
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