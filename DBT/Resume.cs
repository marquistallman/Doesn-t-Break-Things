using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class Resume
{
    public string? Archivo { get; set; }
    public int Bucles { get; private set; }
    public int Declaraciones { get; private set; }
    public int Transformaciones { get; private set; }
    public HashSet<string> ClasesUsadas { get; private set; } = new HashSet<string>();
    public HashSet<string> MetodosUsados { get; private set; } = new HashSet<string>();

    // Palabras clave para excluir de la detección de métodos y tipos
    private static readonly HashSet<string> Keywords = new HashSet<string>
    {
        "if", "else", "for", "foreach", "while", "do", "switch", "return",
        "try", "catch", "finally", "using", "new", "class", "public", "private", 
        "protected", "void", "static", "namespace", "get", "set", "out", "in"
    };

    public void Analizar(List<string> lineas)
    {
        // Reiniciar contadores
        Bucles = 0;
        Declaraciones = 0;
        Transformaciones = 0;
        ClasesUsadas.Clear();
        MetodosUsados.Clear();

        foreach (var linea in lineas)
        {
            string l = linea.Trim();
            if (string.IsNullOrEmpty(l) || l.StartsWith("//")) continue;

            // 1. Detectar Bucles (for, foreach, while)
            if (Regex.IsMatch(l, @"\b(for|foreach|while)\b"))
            {
                Bucles++;
            }

            // 2. Detectar Clases Usadas
            // Instanciación: new Clase(...)
            foreach (Match m in Regex.Matches(l, @"\bnew\s+([A-Z]\w*)"))
            {
                ClasesUsadas.Add(m.Groups[1].Value);
            }
            // Acceso estático: Clase.Metodo (Heurística: Empieza con Mayúscula seguido de punto)
            foreach (Match m in Regex.Matches(l, @"\b([A-Z]\w*)\."))
            {
                string clase = m.Groups[1].Value;
                if (!Keywords.Contains(clase.ToLower())) ClasesUsadas.Add(clase);
            }

            // 3. Detectar Métodos Usados: nombreMetodo(...)
            foreach (Match m in Regex.Matches(l, @"\b([a-zA-Z]\w*)\s*\("))
            {
                string metodo = m.Groups[1].Value;
                if (!Keywords.Contains(metodo)) MetodosUsados.Add(metodo);
            }

            // 4. Declaraciones vs Transformaciones
            // Declaración: Tipo variable = ...; o Tipo variable;
            // Busca: (Tipo o var) espacio (nombreVariable) espacio opcional (= o ;)
            var matchDecl = Regex.Match(l, @"\b(var|int|string|bool|double|float|char|long|List<[^>]+>|[A-Z]\w*)\s+([a-zA-Z_]\w*)\s*(=|;)");
            
            bool esDeclaracion = false;
            if (matchDecl.Success)
            {
                string tipo = matchDecl.Groups[1].Value;
                // Asegurarse de que no sea una instrucción como 'return x;'
                if (!Keywords.Contains(tipo))
                {
                    Declaraciones++;
                    esDeclaracion = true;
                }
            }

            if (!esDeclaracion)
            {
                // Transformación: asignación (=, +=, etc) o incremento/decremento (++, --)
                // Se excluyen comparaciones (==, !=, >=, <=)
                if (Regex.IsMatch(l, @"[^=!><]=[^=]") || Regex.IsMatch(l, @"(\+\+|--)"))
                {
                    Transformaciones++;
                }
            }
        }
    }
}