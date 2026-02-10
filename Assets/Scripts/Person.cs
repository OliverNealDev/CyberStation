using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public abstract class Person : MonoBehaviour
{
    public NavMeshAgent navAgent;
    
    public float comfort = 100f;
    public float satiation = 100f;
    public float hydration = 100f;
    public float hygiene = 100f;

    private float tickLength = 0.1f;
    private float tickTimer = 0f;
    
    public float needReductionRate = 0.5f;

    protected virtual void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = Random.Range(3f, 4f);
        
        comfort = Random.Range(50f, 100f);
        satiation = Random.Range(50f, 100f);
        hydration = Random.Range(50f, 100f);
        hygiene = Random.Range(50f, 100f);
    }

    protected virtual void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickLength)
        {
            HandleNeeds(tickLength);
            OnTick();
            tickTimer = 0f;
        }
    }

    private void HandleNeeds(float delta)
    {
        comfort = Mathf.Max(0f, comfort - delta * needReductionRate);
        satiation = Mathf.Max(0f, satiation - delta * needReductionRate);
        hydration = Mathf.Max(0f, hydration - delta * needReductionRate);
        hygiene = Mathf.Max(0f, hygiene - delta * needReductionRate);
    }

    protected abstract void OnTick();

    public enum NeedType
    {
        None,
        Comfort,
        Satiation,
        Hydration,
        Hygiene
    }

    public List<NeedType> GetNeedsInPriorityOrder()
    {
        List<NeedType> needs = new List<NeedType>
        {
            NeedType.Comfort,
            NeedType.Satiation,
            NeedType.Hydration,
            NeedType.Hygiene
        };
        
        needs.Sort((a, b) =>
        {
            float valueA = GetNeedValue(a);
            float valueB = GetNeedValue(b);
            return valueA.CompareTo(valueB);
        });
        
        return needs;
    }
    
    public float GetNeedValue(NeedType need)
    {
        return need switch
        {
            NeedType.Comfort => comfort,
            NeedType.Satiation => satiation,
            NeedType.Hydration => hydration,
            NeedType.Hygiene => hygiene,
            _ => 0f
        };
    }
}