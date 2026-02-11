using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBT.Services;

public class OllamaImplementPlan : OllamaBridge
{
    public override async Task<string> Ejecutar(string jsonPayload)
    {
        string sourceContext = "";
        string targetContext = "";
        
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonPayload);
            if (doc.RootElement.TryGetProperty("SourceContext", out var s)) sourceContext = s.GetString() ?? "";
            if (doc.RootElement.TryGetProperty("TargetContext", out var t)) targetContext = t.GetString() ?? "";
        }
        catch { }

        var requestBody = new
        {
            model = ModelName,
            prompt = $"[ROLE]: Senior Software Architect\n" +
                     $"[TASK]: Analyze the SOURCE requirements and TARGET project structure. Create a detailed plan of files to create or modify.\n" +
                     $"[OUTPUT]: A JSON Array where each item represents a file.\n" +
                     $"[EXAMPLE]: \n" +
                     $"[\n" +
                     $"  {{ \"name\": \"src/math_parser.py\", \"instructions\": \"Define class 'MathParser' with method 'parse'. No external imports needed.\" }},\n" +
                     $"  {{ \"name\": \"src/main.py\", \"instructions\": \"Import 'MathParser' from 'src.math_parser'. Call 'MathParser.parse'.\" }}\n" +
                     $"]\n" +
                     $"[SOURCE]:\n{sourceContext}\n" +
                     $"[TARGET]:\n{targetContext}\n" +
                     $"[IMPORTANT]: \n" +
                     $"1. Return ONLY the JSON array.\n" +
                     $"2. Use the programming language and file extensions specified in SOURCE (e.g. .py, .js, .cs).\n" +
                     $"3. Do NOT use generic instructions. Extract specific requirements for each file from SOURCE.\n" +
                     $"4. In 'instructions', explicitly specify what to import (and from where) and enforce exact function/class names to be defined in that file.",
            stream = false,
            format = "json" // Fuerza al modelo a responder en JSON válido
        };

        string jsonString = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"\n[OllamaImplementPlan] Generando plan de archivos con '{ModelName}'...");
            var response = await Client.PostAsync(Url, content);
            response.EnsureSuccessStatusCode();
            
            string responseString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseString);
            return doc.RootElement.GetProperty("response").GetString() ?? "[]";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}