
using System.Collections.Generic;
using UnityEngine;


public class FormationDetector : MonoBehaviour
{
    public NodeMap nodeMap;
    public List<Pattern> patterns = new List<Pattern>();

    [System.Serializable]
    public class Pattern
    {
        public string name;
        public List<IngredientesSO> ingredients = new List<IngredientesSO>();

    }

}