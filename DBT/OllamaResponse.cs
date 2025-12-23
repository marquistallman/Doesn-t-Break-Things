using System;
using System.Text.Json;
using System.Threading.Tasks;

// Clase para la Respuesta: Procesa el JSON crudo de Ollama y extrae el texto
public class OllamaResponse : OllamaBridge
{
    public override async Task<string> Ejecutar(string rawJsonResponse)
    {
        if (rawJsonResponse.StartsWith("Error")) return rawJsonResponse;

        try
        {
            using JsonDocument doc = JsonDocument.Parse(rawJsonResponse);
            if (doc.RootElement.TryGetProperty("response", out JsonElement responseElement))
            {
                return responseElement.GetString() ?? "La respuesta del modelo estaba vacía.";
            }
            return "No se encontró el campo 'response' en el JSON de Ollama.";
        }
        catch (Exception ex)
        {
            return $"Error al procesar la respuesta: {ex.Message}";
        }
    }
}