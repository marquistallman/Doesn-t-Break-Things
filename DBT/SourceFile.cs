using System;
using System.Collections.Generic;
using System.IO;

public class SourceFile
{
    public List<string> Lineas { get; private set; }
    public string Lenguaje { get; private set; }
    public string Ruta { get; private set; }

    private static readonly Dictionary<string, string> _extensionMap = new Dictionary<string, string>
    {
        { ".cs", "C#" }, { ".java", "Java" }, { ".py", "Python" },
        { ".js", "JavaScript" }, { ".ts", "TypeScript" }, { ".cpp", "C++" },
        { ".c", "C" }, { ".html", "HTML" }, { ".css", "CSS" }, { ".sql", "SQL" },
        { ".json", "JSON" }, { ".xml", "XML" }, { ".yaml", "YAML" }, { ".yml", "YAML" },
        { ".csproj", "C# Project" }, { ".sln", "Solution" }, { ".md", "Markdown" }
    };

    public SourceFile(string ruta)
    {
        Ruta = ruta;
        // File.ReadAllLines lanza FileNotFoundException automáticamente si el archivo no existe
        Lineas = new List<string>(File.ReadAllLines(ruta));
        Lenguaje = IdentificarLenguaje(ruta);
    }

    public static string IdentificarLenguaje(string ruta)
    {
        return _extensionMap.TryGetValue(Path.GetExtension(ruta).ToLower(), out var lang) ? lang : "Desconocido";
    }
}
