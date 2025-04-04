using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "resource", menuName = "Tools/resources")]

public abstract class ResourcesSO : ScriptableObject
{
    public string Name;
    public string Description;
    public float Price;
    public abstract void ActivarEfecto(List<Node> neighbors);

}
