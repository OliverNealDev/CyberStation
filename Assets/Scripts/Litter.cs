using UnityEngine;

public class Litter : MonoBehaviour
{
    public float timeToClean = 5;

    void Start()
    {
        JanitorCoordinator.Instance.ReportLitter(this);
    }
}
