using System.Collections.Generic;
using UnityEngine;

public class GlobalPersonVisuals : MonoBehaviour
{
    public List<Material> skinMaterials = new List<Material>();
    public List<Material> hairMaterials = new List<Material>();
    
    public PersonVisualsData securityVisualsData;
    public PersonVisualsData janitorVisualsData;
    
    public static GlobalPersonVisuals Instance;
    
    void Awake()
    {
        Instance = this;
    }
    
    public Material GetRandomSkinMaterial()
    {
        if (skinMaterials.Count == 0) return null;
        return skinMaterials[Random.Range(0, skinMaterials.Count)];
    }
    
    public Material GetRandomHairMaterial()
    {
        if (hairMaterials.Count == 0) return null;
        return hairMaterials[Random.Range(0, hairMaterials.Count)];
    }
}
