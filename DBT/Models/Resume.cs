using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace DBT.Models;

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

    // Optimización: Regex compilados y estáticos para evitar overhead en cada línea
    private static readonly Regex _loopRegex = new Regex(@"\b(for|foreach|while)\b", RegexOptions.Compiled);
    private static readonly Regex _newClassRegex = new Regex(@"\bnew\s+([A-Z]\w*)", RegexOptions.Compiled);
    private static readonly Regex _staticCallRegex = new Regex(@"\b([A-Z]\w*)\.", RegexOptions.Compiled);
    private static readonly Regex _methodCallRegex = new Regex(@"\b([a-zA-Z]\w*)\s*\(", RegexOptions.Compiled);
    private static readonly Regex _declarationRegex = new Regex(@"\b(var|int|string|bool|double|float|char|long|List<[^>]+>|[A-Z]\w*)\s+([a-zA-Z_]\w*)\s*(=|;)", RegexOptions.Compiled);
    private static readonly Regex _transformRegex = new Regex(@"[^=!><]=[^=]", RegexOptions.Compiled);
    private static readonly Regex _incDecRegex = new Regex(@"(\+\+|--)", RegexOptions.Compiled);

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
            if (_loopRegex.IsMatch(l))
            {
                Bucles++;
            }

            // 2. Detectar Clases Usadas
            // Instanciación: new Clase(...)
            foreach (Match m in _newClassRegex.Matches(l))
            {
                ClasesUsadas.Add(m.Groups[1].Value);
            }
            // Acceso estático: Clase.Metodo (Heurística: Empieza con Mayúscula seguido de punto)
            foreach (Match m in _staticCallRegex.Matches(l))
            {
                string clase = m.Groups[1].Value;
                if (!Keywords.Contains(clase.ToLower())) ClasesUsadas.Add(clase);
            }

            // 3. Detectar Métodos Usados: nombreMetodo(...)
            foreach (Match m in _methodCallRegex.Matches(l))
            {
                string metodo = m.Groups[1].Value;
                if (!Keywords.Contains(metodo)) MetodosUsados.Add(metodo);
            }

            // 4. Declaraciones vs Transformaciones
            // Declaración: Tipo variable = ...; o Tipo variable;
            // Busca: (Tipo o var) espacio (nombreVariable) espacio opcional (= o ;)
            var matchDecl = _declarationRegex.Match(l);
            
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
                if (_transformRegex.IsMatch(l) || _incDecRegex.IsMatch(l))
                {
                    Transformaciones++;
                }
            }
        }
    }
}