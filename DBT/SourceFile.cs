using System;
using System.Collections.Generic;
using System.IO;

public class SourceFile
{
    public List<string> Lineas { get; private set; }
    public string Lenguaje { get; private set; }
    public string Ruta { get; private set; }

    public SourceFile(string ruta)
    {
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException("El archivo especificado no existe.", ruta);
        }

        Ruta = ruta;
        Lineas = new List<string>(File.ReadAllLines(ruta));
        Lenguaje = IdentificarLenguaje(ruta);
    }

    public static string IdentificarLenguaje(string ruta)
    {
        string extension = Path.GetExtension(ruta).ToLower();
        return extension switch
        {
            ".cs" => "C#",
            ".java" => "Java",
            ".py" => "Python",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".cpp" => "C++",
            ".c" => "C",
            ".html" => "HTML",
            ".css" => "CSS",
            ".sql" => "SQL",
            _ => "Desconocido"
        };
    }
}
