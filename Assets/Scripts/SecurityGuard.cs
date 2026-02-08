using UnityEngine;
using UnityEngine.AI;

public class SecurityGuard : Staff
{
    public NavMeshAgent agent;
    
    public securitySubStates currentSubState = securitySubStates.Idle;
    public enum securitySubStates
    {
        Idle,
        MovingToTarget,
        InteractingWithTarget
    }
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = Random.Range(4.5f, 5f);
    }
}
