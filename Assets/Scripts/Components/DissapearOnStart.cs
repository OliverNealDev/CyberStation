using UnityEngine;

public class DissapearOnStart : MonoBehaviour
{
    [SerializeField] private bool dissapearOnStart = true;
    
    void Start()
    {
        if (dissapearOnStart)
        {
            GetComponent<MeshRenderer>().enabled = false;
        }
    }
}
