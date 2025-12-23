using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class SummarizeTool : Tools
{
    public override async Task Ejecutar(string[] args)
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
                await ShowFileData(filePath, ollama);
            }
            else if (Directory.Exists(filePath))
            {
                await SaveFileData(filePath, ollama);
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

    private async Task ShowFileData(string filePath, OllamaInput ollama)
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

    private async Task SaveFileData(string directoryPath, OllamaInput ollama)
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
}