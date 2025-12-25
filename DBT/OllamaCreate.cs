using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DBT;

public class OllamaCreate : OllamaBridge
{
    public override async Task<string> Ejecutar(string idea)
    {
        string prompt = $@"
Act as a Senior Software Architect.
Analyze the following project idea and generate a comprehensive 'requirements.txt' file.
The file should outline the project structure, necessary files, and a brief description of what each file should contain.
This output will be used by an automated implementation tool to generate the code.

Idea:
{idea}

Output ONLY the content of the requirements.txt file. Do not include markdown code blocks like ```txt. Just the raw content.
";

        var payload = new
        {
            model = ModelName,
            prompt = prompt,
            stream = false
        };

        string json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await Client.PostAsync(Url, content);
            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseString);
            string text = doc.RootElement.GetProperty("response").GetString() ?? "";
            
            // Limpieza básica
            return text.Replace("```txt", "").Replace("```", "").Trim();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error comunicando con Ollama: {ex.Message}");
        }
    }
}