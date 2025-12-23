using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Clase abstracta base
public abstract class OllamaBridge
{
    protected string ModelName { get; private set; } = "llama3"; // Modelo por defecto
    protected static readonly HttpClient Client = new HttpClient();
    protected const string Url = "http://localhost:11434/api/generate";

    // Método general para escoger el modelo
    public async Task SetModel()
    {
        string configFile = "properties.json";
        bool modelFound = false;

        if (File.Exists(configFile))
        {
            try
            {
                string json = await File.ReadAllTextAsync(configFile);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("model", out JsonElement modelElement))
                {
                    string? model = modelElement.GetString();
                    if (!string.IsNullOrWhiteSpace(model))
                    {
                        ModelName = model;
                        Console.WriteLine($"Modelo cargado desde {configFile}: {ModelName}");
                        modelFound = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer {configFile}: {ex.Message}");
            }
        }

        if (!modelFound)
        {
            Console.WriteLine("Buscando modelos disponibles en Ollama...");
            try
            {
                var response = await Client.GetAsync("http://localhost:11434/api/tags");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement))
                {
                    var models = new List<string>();
                    int i = 1;
                    foreach (var model in modelsElement.EnumerateArray())
                    {
                        string name = model.GetProperty("name").GetString() ?? "Desconocido";
                        models.Add(name);
                        Console.WriteLine($"{i}. {name}");
                        i++;
                    }

                    if (models.Count > 0)
                    {
                        Console.Write("Seleccione el número del modelo: ");
                        if (int.TryParse(Console.ReadLine(), out int selection) && selection >= 1 && selection <= models.Count)
                        {
                            ModelName = models[selection - 1];
                            Console.WriteLine($"Modelo seleccionado: {ModelName}");
                        }
                        else
                        {
                            Console.WriteLine("Selección inválida. Se usará el modelo por defecto.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No se encontraron modelos instalados en Ollama.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo conectar con Ollama para listar modelos: {ex.Message}");
            }
        }
    }

    // Método abstracto que heredarán las clases de entrada y respuesta
    public abstract Task<string> Ejecutar(string contenido);
}

// Clase para la Entrada: Prepara el prompt y lo envía a Ollama
public class OllamaInput : OllamaBridge
{
    public override async Task<string> Ejecutar(string jsonAnalysis)
    {
        var requestBody = new
        {
            model = ModelName,
            prompt = $"Eres un experto en análisis de código. Basándote en el siguiente análisis JSON de un proyecto de software, genera un reporte técnico breve sobre la complejidad y las dependencias del código:\n\n{jsonAnalysis}",
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