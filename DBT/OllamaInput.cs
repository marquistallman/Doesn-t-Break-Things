using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Clase para la Entrada: Prepara el prompt y lo envía a Ollama
public class OllamaInput : OllamaBridge
{
    public override async Task<string> Ejecutar(string jsonAnalysis)
    {
        var requestBody = new
        {
            model = ModelName,
            prompt = $"Actúa como un arquitecto de software experto. Genera un resumen técnico asertivo del siguiente código basado en sus metadatos. \n\nInstrucciones:\n1. Describe directamente qué hace el código (ej: 'Este código es un gestor de...').\n2. NO utilices palabras de incertidumbre como 'parece', 'podría', 'probablemente' o 'quizás'.\n3. Menciona explícitamente las clases principales y qué funcionalidad implementan.\n4. Enfócate en la arquitectura y el flujo lógico deducido.\n\nDatos del análisis:\n{jsonAnalysis}",
            stream = false
        };

        string jsonString = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"\n[OllamaInput] Enviando análisis al modelo '{ModelName}'...");
            var response = await Client.PostAsync(Url, content);
            response.EnsureSuccessStatusCode();
            
            // Devuelve la respuesta cruda (JSON completo de Ollama)
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}