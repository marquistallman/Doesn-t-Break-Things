// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace DBT;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Introduce la ruta del archivo de código:");
        string? filePath = Console.ReadLine();

        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                showFileData(filePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error al leer el archivo: {ex.Message}");
            }
        }
        else if(!string.IsNullOrWhiteSpace(filePath) && Directory.Exists(filePath))
        {
            try
            {
                saveFileData(filePath);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error al leer el archivo: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("La ruta del archivo no es válida o el archivo no existe.");
        }
    }
    public static void showFileData(string filePath)
    {
        SourceFile archivo = new SourceFile(filePath);
        Console.WriteLine($"Se han guardado {archivo.Lineas.Count} líneas en la lista.");
            Console.WriteLine($"Lenguaje detectado: {archivo.Lenguaje}");

            Resume resume = new Resume();
            resume.Archivo = archivo.Ruta;
            resume.Analizar(archivo.Lineas);
            Console.WriteLine("\n--- Resumen del Análisis ---");
            Console.WriteLine($"Bucles detectados: {resume.Bucles}");
            Console.WriteLine($"Declaraciones de variables: {resume.Declaraciones}");
            Console.WriteLine($"Transformaciones de variables: {resume.Transformaciones}");
            Console.WriteLine($"Clases usadas: {string.Join(", ", resume.ClasesUsadas)}");
            Console.WriteLine($"Métodos usados: {string.Join(", ", resume.MetodosUsados)}");
            Console.WriteLine("----------------------------\n");
    }
    public static void saveFileData(string directoryPath)
    {
        List<Resume> resumes = new List<Resume>();
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
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error al leer el archivo {filePath}: {ex.Message}");
            }
        }

        string salida = JsonSerializer.Serialize(resumes, new JsonSerializerOptions { WriteIndented = true });
        string outputFilePath = "analysis_results.json";
        File.WriteAllText(outputFilePath, salida);
        Console.WriteLine($"\nLa información se ha guardado correctamente en: {Path.GetFullPath(outputFilePath)}");
    }
}
