using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Clase para la herramienta Fix: Solicita correcciones de código
public class OllamaFix : OllamaBridge
{
    public override async Task<string> Ejecutar(string jsonPayload)
    {
        // 1. Extraer el código real del JSON para que el modelo lo vea limpio (sin escapes \r\n de JSON)
        string codigoLimpio = "";
        string problemas = "";
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonPayload);
            if (doc.RootElement.TryGetProperty("CodigoPrincipal", out JsonElement codeEl))
                codigoLimpio = codeEl.GetString() ?? "";
            
            // Opcional: Extraer problemas para dar contexto
            if (doc.RootElement.TryGetProperty("Problemas", out JsonElement probEl) && probEl.ValueKind == JsonValueKind.Array)
            {
                foreach(var p in probEl.EnumerateArray())
                    problemas += $"- {p.GetProperty("Nombre").GetString()} (Línea {p.GetProperty("Linea").GetInt32()})\n";
            }
        }
        catch { codigoLimpio = jsonPayload; } // Fallback

        var requestBody = new
        {
            model = ModelName,
            prompt = $"[ROLE]: Expert Code Fixer\n" +
                     $"[TASK]: Fix the C# code provided below. Output ONLY the corrected code block.\n" +
                     $"[RULES]:\n" +
                     $"1. Use a markdown code block (```).\n" +
                     $"2. RETURN CODE ONLY. Do NOT add comments at the end of lines (e.g. 'int x = 0; // Fixed').\n" +
                     $"3. REMOVE existing error comments from the input.\n" +
                     $"4. Maintain original indentation and newlines.\n" +
                     (string.IsNullOrEmpty(problemas) ? "" : $"\n[ISSUES DETECTED]:\n{problemas}") +
                     $"\n[CODE TO FIX]:\n" +
                     $"{codigoLimpio}\n\n" + // Enviamos el código como texto plano, no JSON
                     $"[CORRECTED CODE]:",
            stream = false,
            options = new { temperature = 0.1 } // Temperatura baja para mayor precisión
        };

        string jsonString = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"\n[OllamaFix] Consultando solución al modelo '{ModelName}'...");
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
