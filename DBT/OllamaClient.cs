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
        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        string configFile = Path.Combine(exePath, "properties.json");
        List<string> availableModels = new List<string>();
        bool ollamaReachable = false;

        // 1. Obtener modelos disponibles de Ollama
        try
        {
            var response = await Client.GetAsync("http://localhost:11434/api/tags");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("models", out JsonElement modelsElement))
            {
                foreach (var model in modelsElement.EnumerateArray())
                {
                    string name = model.GetProperty("name").GetString() ?? "";
                    if (!string.IsNullOrEmpty(name)) availableModels.Add(name);
                }
            }
            ollamaReachable = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Advertencia: No se pudo conectar con Ollama para listar modelos ({ex.Message})");
        }

        // 2. Intentar cargar y validar desde properties.json
        bool modelLoaded = false;
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
                        // Si tenemos conexión, validamos que el modelo exista
                        if (ollamaReachable && !availableModels.Contains(model))
                        {
                            Console.WriteLine($"El modelo '{model}' configurado en {configFile} no está instalado.");
                            await UpdateConfigFile(configFile, null); // Borrar del JSON
                        }
                        else
                        {
                            ModelName = model;
                            Console.WriteLine($"Modelo cargado desde {configFile}: {ModelName}");
                            modelLoaded = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al leer {configFile}: {ex.Message}");
            }
        }

        // 3. Si no se cargó modelo válido, pedir al usuario
        if (!modelLoaded)
        {
            if (ollamaReachable && availableModels.Count > 0)
            {
                Console.WriteLine("Modelos disponibles en Ollama:");
                for (int i = 0; i < availableModels.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {availableModels[i]}");
                }

                if (availableModels.Count > 0)
                {
                    Console.Write("Seleccione el número del modelo: ");
                    if (int.TryParse(Console.ReadLine(), out int selection) && selection >= 1 && selection <= availableModels.Count)
                    {
                        ModelName = availableModels[selection - 1];
                        Console.WriteLine($"Modelo seleccionado: {ModelName}");
                        await UpdateConfigFile(configFile, ModelName); // Guardar selección
                    }
                    else
                    {
                        Console.WriteLine("Selección inválida. Se usará el modelo por defecto.");
                    }
                }
            }
            else if (!ollamaReachable)
            {
                Console.WriteLine("No se puede seleccionar un modelo (sin conexión).");
            }
            else
            {
                Console.WriteLine("No se encontraron modelos instalados en Ollama.");
            }
        }
    }

    private async Task UpdateConfigFile(string configFile, string? modelName)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        Dictionary<string, object> config = new Dictionary<string, object>();

        if (File.Exists(configFile))
        {
            try
            {
                string json = await File.ReadAllTextAsync(configFile);
                if (!string.IsNullOrWhiteSpace(json))
                    config = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch { }
        }

        if (modelName != null)
        {
            config["model"] = modelName;
            Console.WriteLine($"Guardando configuración en {configFile}...");
        }
        else if (config.ContainsKey("model"))
        {
            config.Remove("model");
            Console.WriteLine($"Eliminando modelo inválido de {configFile}...");
        }

        await File.WriteAllTextAsync(configFile, JsonSerializer.Serialize(config, options));
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