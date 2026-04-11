using System.Collections.Generic;
using UnityEngine;

public class GenericTrainCarriageData : MonoBehaviour
{
    [SerializeField] private List<MeshRenderer> changeableMeshRenderers = new List<MeshRenderer>();

    public List<MeshRenderer> ChangeableMeshRenderers => changeableMeshRenderers;
}
