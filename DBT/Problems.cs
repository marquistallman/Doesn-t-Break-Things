using System;
using System.Collections.Generic;
using System.Linq;

public class Problems
{
    public string RutaArchivo { get; private set; }
    public string Lenguaje { get; private set; }
    public List<ProblemDetail> ListaProblemas { get; private set; } = new List<ProblemDetail>();

    // Método para recibir problemas desde el editor y extraer el código del SourceFile
    public void CargarProblemas(SourceFile archivo, List<(string nombre, int linea)> problemas)
    {
        RutaArchivo = archivo.Ruta;
        Lenguaje = archivo.Lenguaje;

        ListaProblemas = problemas.Select(p => new ProblemDetail
        {
            Nombre = p.nombre,
            Linea = p.linea,
            Codigo = (p.linea > 0 && p.linea <= archivo.Lineas.Count) 
                     ? archivo.Lineas[p.linea - 1].Trim() 
                     : string.Empty
        }).ToList();
    }
}
