using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PersonVisualsData", menuName = "Scriptable Objects/PersonVisualsData")]
public class PersonVisualsData : ScriptableObject
{
    public List<GameObject> BodyModels = new List<GameObject>();
    public List<GameObject> HairModels = new List<GameObject>();
    public List<GameObject> HeadModels = new List<GameObject>();
    
    public GameObject GetRandomBodyModel()
    {
        if (BodyModels.Count == 0) return null;
        return BodyModels[Random.Range(0, BodyModels.Count)];
    }
    
    public GameObject GetRandomHairModel()
    {
        if (HairModels.Count == 0) return null;
        return HairModels[Random.Range(0, HairModels.Count)];
    }
    
    public GameObject GetRandomHeadModel()
    {
        if (HeadModels.Count == 0) return null;
        return HeadModels[Random.Range(0, HeadModels.Count)];
    }
}
