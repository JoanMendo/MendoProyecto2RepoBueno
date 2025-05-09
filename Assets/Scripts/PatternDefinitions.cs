using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static PatternDefinitions.Patron;

/// ‡‡<summary>_PLACEHOLDER‡‡
/// Contiene las definiciones de patrones y efectos para el sistema de detección
/// ‡‡</summary>_PLACEHOLDER‡‡
public static class PatternDefinitions
{
    // Enumeración para los tipos de efectos disponibles
    public enum TipoEfecto
    {
        ModificarValor,      // Aumenta o disminuye un valor numérico
        CambiarIngrediente,  // Transforma el ingrediente en otro tipo
        AgregarEfectoVisual, // Solo añade un efecto visual al ingrediente
        EliminarIngrediente  // Elimina el ingrediente del nodo
    }

    // Clase que define un efecto que puede aplicarse a un ingrediente
    [System.Serializable]
    public class EfectoPatron
    {
        public string nombre;
        public TipoEfecto tipo;
        public float valorModificacion;
        public string parametroObjetivo; // El nombre del parámetro a modificar (si aplica)
        public string nuevoIngrediente;  // El nombre del nuevo ingrediente (si aplica)
        public Color colorEfecto = Color.cyan;

        // Aplicar este efecto a un nodo
        public void AplicarA(GameObject nodo)
        {
            if (nodo == null) return;

            // Obtener componente Node
            Node nodeComp = nodo.GetComponent<Node>();
            if (nodeComp == null || nodeComp.currentIngredient == null) return;

            // Aplicar el efecto según su tipo
            switch (tipo)
            {
                case TipoEfecto.ModificarValor:
                    // Aquí modificaríamos valores del ingrediente
                    // Por ejemplo: nodeComp.currentIngredient.GetComponent<IngredienteSO>().potencia += valorModificacion;
                    Debug.Log($"Aplicando modificación de {valorModificacion} a {parametroObjetivo}");
                    break;

                case TipoEfecto.CambiarIngrediente:
                    // Cambiar el tipo de ingrediente
                    Debug.Log($"Transformando ingrediente en {nuevoIngrediente}");
                    break;

                case TipoEfecto.AgregarEfectoVisual:
                    // Solo efecto visual
                    Debug.Log($"Aplicando efecto visual {nombre}");
                    // Aquí agregaríamos partículas o cambios visuales
                    break;

                case TipoEfecto.EliminarIngrediente:
                    // Eliminar el ingrediente
                    nodeComp.ClearNodeIngredient();
                    Debug.Log("Ingrediente eliminado");
                    break;
            }

            // Destacar el nodo al que se aplica el efecto (esto asumiría que añadirás un método destacar)
            // nodeComp.Highlight(true, colorEfecto, 1.5f);
        }
    }

    // Clase que define un patrón a detectar en el tablero
    [System.Serializable]
    public class Patron
    {
        public string nombre;
        public List<Vector2Int> posiciones = new List<Vector2Int>();
        public List<string> ingredientesRequeridos = new List<string>();

        // Define qué nodos serán afectados por el patrón
        public enum ModoObjetivo
        {
            TodosLosNodos,    // Todos los nodos del patrón
            NodoCentral,      // Solo el nodo en el centro (0,0)
            PosicionesEspecificas // Nodos en posiciones específicas
        }

        public ModoObjetivo modoObjetivo = ModoObjetivo.NodoCentral;
        public List<Vector2Int> posicionesObjetivo = new List<Vector2Int>();

        // Efecto a aplicar en los nodos objetivo
        public EfectoPatron efecto;

        // Generación de transformaciones (rotaciones y reflejos)
        public List<List<Vector2Int>> GenerarTransformaciones()
        {
            List<List<Vector2Int>> transformaciones = new List<List<Vector2Int>>();

            // Original
            transformaciones.Add(new List<Vector2Int>(posiciones));

            // Rotaciones 90°, 180°, 270°
            List<Vector2Int> rotacion90 = posiciones.Select(p => new Vector2Int(p.y, -p.x)).ToList();
            List<Vector2Int> rotacion180 = posiciones.Select(p => new Vector2Int(-p.x, -p.y)).ToList();
            List<Vector2Int> rotacion270 = posiciones.Select(p => new Vector2Int(-p.y, p.x)).ToList();

            transformaciones.Add(rotacion90);
            transformaciones.Add(rotacion180);
            transformaciones.Add(rotacion270);

            // Reflejos en X e Y
            List<Vector2Int> reflejoX = posiciones.Select(p => new Vector2Int(-p.x, p.y)).ToList();
            List<Vector2Int> reflejoY = posiciones.Select(p => new Vector2Int(p.x, -p.y)).ToList();

            transformaciones.Add(reflejoX);
            transformaciones.Add(reflejoY);

            return transformaciones;
        }
    }

    // Clase para almacenar un patrón detectado
    public class PatronDetectado
    {
        public string nombre;
        public List<GameObject> nodos = new List<GameObject>();
        public List<GameObject> nodosObjetivo = new List<GameObject>();
        public EfectoPatron efecto;

        // Aplicar efecto a todos los nodos objetivo
        public void AplicarEfecto()
        {
            foreach (GameObject nodo in nodosObjetivo)
            {
                efecto.AplicarA(nodo);
            }
        }
    }

    // Lista de patrones predefinidos
    public static List<Patron> ObtenerPatronesDisponibles()
    {
        List<Patron> patrones = new List<Patron>();

        // Patrón: Cruz
        patrones.Add(new Patron
        {
            nombre = "Cruz",
            posiciones = new List<Vector2Int>
            {
                new Vector2Int(0, 0),   // Centro
                new Vector2Int(1, 0),   // Derecha
                new Vector2Int(-1, 0),  // Izquierda
                new Vector2Int(0, 1),   // Arriba
                new Vector2Int(0, -1)   // Abajo
            },
            ingredientesRequeridos = new List<string> { "Tomate", "Cebolla" },
            modoObjetivo = ModoObjetivo.NodoCentral,
            efecto = new EfectoPatron
            {
                nombre = "Potenciación",
                tipo = TipoEfecto.ModificarValor,
                parametroObjetivo = "potencia",
                valorModificacion = 2.0f,
                colorEfecto = Color.yellow
            }
        });

        // Patrón: Cuadrado
        patrones.Add(new Patron
        {
            nombre = "Cuadrado",
            posiciones = new List<Vector2Int>
            {
                new Vector2Int(0, 0),   // Esquina inferior izquierda
                new Vector2Int(1, 0),   // Esquina inferior derecha
                new Vector2Int(0, 1),   // Esquina superior izquierda
                new Vector2Int(1, 1)    // Esquina superior derecha
            },
            ingredientesRequeridos = new List<string> { "Queso" },
            modoObjetivo = ModoObjetivo.TodosLosNodos,
            efecto = new EfectoPatron
            {
                nombre = "Mejora Completa",
                tipo = TipoEfecto.AgregarEfectoVisual,
                colorEfecto = Color.blue
            }
        });

        return patrones;
    }
}