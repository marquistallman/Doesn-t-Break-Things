using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Clase para la herramienta Implement: Genera código basado en requisitos y contexto
public class OllamaImplement : OllamaBridge
{
    public override async Task<string> Ejecutar(string jsonPayload)
    {
        string sourceContext = "";
        string targetContext = "";
        
        // Extraer contextos del payload
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
            prompt = $"[ROLE]: Senior Software Architect & Developer\n" +
                     $"[TASK]: Implement the functionality described in the SOURCE into the TARGET project.\n" +
                     $"[RULES]:\n" +
                     $"1. Analyze the SOURCE (requirements or code) and TARGET (existing project structure).\n" +
                     $"2. Output the FULL content of new files or modified files needed for the implementation.\n" +
                     $"3. CRITICAL: Start every file block with a comment line exactly like this: `// FILE: path/to/file.ext` (use relative paths).\n" +
                     $"4. Use markdown code blocks (```).\n" +
                     $"5. Ensure the code is complete, compilable, and integrates with the existing Target structure.\n" +
                     $"6. If modifying an existing file, provide the complete new content of that file.\n" +
                     $"\n[SOURCE CONTEXT]:\n{sourceContext}\n" +
                     $"\n[TARGET PROJECT CONTEXT]:\n{targetContext}\n" +
                     $"\n[OUTPUT]:",
            stream = false,
            options = new { temperature = 0.2 } // Baja temperatura para seguir instrucciones estrictas
        };

        string jsonString = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"\n[OllamaImplement] Generando implementación con modelo '{ModelName}'...");
            var response = await Client.PostAsync(Url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}