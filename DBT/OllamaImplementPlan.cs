using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBT;

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
                     $"  {{ \"name\": \"src/main.py\", \"instructions\": \"Implement the main entry point. Import math_parser. Initialize the app...\" }},\n" +
                     $"  {{ \"name\": \"tests/test_main.py\", \"instructions\": \"Create unit tests for the main module...\" }}\n" +
                     $"]\n" +
                     $"[SOURCE]:\n{sourceContext}\n" +
                     $"[TARGET]:\n{targetContext}\n" +
                     $"[IMPORTANT]: \n" +
                     $"1. Return ONLY the JSON array.\n" +
                     $"2. Use the programming language and file extensions specified in SOURCE (e.g. .py, .js, .cs).\n" +
                     $"3. Do NOT use generic instructions. Extract specific requirements for each file from SOURCE.",
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