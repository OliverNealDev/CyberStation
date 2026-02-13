using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;

public abstract class Person : MonoBehaviour
{
    public NavMeshAgent navAgent;
    
    [SerializeField] private GameObject PersonalCanvasPrefab;
    
    public float comfort = 100f;
    public float satiation = 100f;
    public float hydration = 100f;
    public float hygiene = 100f;

    private float tickLength = 0.1f;
    private float tickTimer = 0f;
    
    public float needReductionRate = 0.5f;

    public GameObject personalCanvas;
    public DialogueData dialogueData;

    public Vector3 PreviousPosition;

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
    
    public virtual void CreateNewPersonalCanvas(Person person)
    {
        if (person == null) return;
        
        if (person.personalCanvas != null)
        {
            Destroy(person.personalCanvas.gameObject);
        }

        GameObject personalCanvas = Instantiate(PersonalCanvasPrefab);
        person.personalCanvas = personalCanvas;
        personalCanvas.transform.SetParent(person.transform, false);
        personalCanvas.transform.localPosition = Vector3.up * 5f;
    }

    public virtual void DestroyPersonalCanvas(Person person)
    {
        if (person.personalCanvas != null)
        {
            Destroy(person.personalCanvas.gameObject);
            person.personalCanvas = null;
        }
    }

    public virtual void Dialogue(Person person, string text, Color color, float duration)
    {
        CreateNewPersonalCanvas(person);

        if (text.Length != 0)
        {
            TextMeshProUGUI dialogueText = person.personalCanvas.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            dialogueText.color = color;
            dialogueText.text = text;
            person.personalCanvas.transform.GetChild(0).gameObject.SetActive(true);
        }
        
        StartCoroutine(ExecuteAfterDelay(duration, () => DestroyPersonalCanvas(person)));
    }
    
    public virtual void Expression(Person person, string expressionName, float duration)
    {
        CreateNewPersonalCanvas(person);

        if (expressionName.Length != 0)
        {
            Image expressionImage = person.personalCanvas.transform.GetChild(1).GetComponent<Image>();
            person.personalCanvas.transform.GetChild(1).gameObject.SetActive(true);
        }
        
        StartCoroutine(ExecuteAfterDelay(duration, () => DestroyPersonalCanvas(person)));
    }

    /*private void CheckIfStuck()
    {
        float distance = Vector3.Distance(PreviousPosition, transform.position);
        if (distance < 0.1f)
        {
            if (navAgent.obstacleAvoidanceType != ObstacleAvoidanceType.NoObstacleAvoidance)
            {
                navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                Invoke("EnableObstacleAvoidance", 1f);
            }
        }
        PreviousPosition = transform.position;
        Invoke("CheckIfStuck", 1f);
    }
    
    private void EnableObstacleAvoidance()
    {
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }*/
    
    public static System.Collections.IEnumerator ExecuteAfterDelay(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }
}