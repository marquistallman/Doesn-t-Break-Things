using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBT;

public class OllamaImplementFile : OllamaBridge
{
    // Método específico para generar un archivo individual
    public async Task<string> GenerarArchivo(string path, string instruction, string sourceContext, string targetContext)
    {
        var requestBody = new
        {
            model = ModelName,
            prompt = $"[ROLE]: Senior Developer\n" +
                     $"[TASK]: Implement the code for the file: '{path}'.\n" +
                     $"[INSTRUCTION]: {instruction}\n" +
                     $"[CONTEXT]:\n" +
                     $"Requirements: {sourceContext}\n" +
                     $"Project Structure: {targetContext}\n" +
                     $"[RULES]:\n" +
                     $"1. Output ONLY the code for the file.\n" +
                     $"2. Do not include markdown code blocks (```) if possible. Just the raw code.\n" +
                     $"3. Ensure the code is complete, compilable, and follows the instructions.\n",
            stream = false
        };

        string jsonString = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            var response = await Client.PostAsync(Url, content);
            response.EnsureSuccessStatusCode();
            
            string responseString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseString);
            string text = doc.RootElement.GetProperty("response").GetString() ?? "";
            
            // Limpieza de markdown por si acaso
            text = text.Replace("```csharp", "").Replace("```cs", "").Replace("```json", "").Replace("```", "").Trim();
            return text;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generando archivo {path}: {ex.Message}");
        }
    }

    public override Task<string> Ejecutar(string contenido)
    {
        throw new NotImplementedException("Use GenerarArchivo instead.");
    }
}