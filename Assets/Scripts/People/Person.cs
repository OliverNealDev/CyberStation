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
    
    public float needReductionRate = 0.5f;

    public GameObject personalCanvas;
    public DialogueData dialogueData;
    public ExpressionData expressionData;
    
    private float tickLength = 0.1f;
    private float tickTimer = 0f;

    public Vector3 PreviousPosition;

    protected virtual void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = Random.Range(3f, 4f);
    }

    void Start()
    {
        float randomScale = Random.Range(0.9f, 1.1f);
        transform.localScale = new Vector3(randomScale, randomScale, randomScale);
    }

    protected abstract void OnTick(float tickLength);

    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickLength)
        {
            OnTick(tickTimer);
            tickTimer = 0f;
        }
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
        
        if (person.personalCanvas != null)
        {
            if (text.Length != 0 && person.personalCanvas != null)
            {
                TextMeshProUGUI dialogueText = person.personalCanvas.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
                dialogueText.color = color;
                dialogueText.text = text;
                person.personalCanvas.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
            }
        
            StartCoroutine(ExecuteAfterDelay(duration, () => DestroyPersonalCanvas(person)));
        }
    }
    
    public virtual void Expression(Person person, Sprite sprite, float duration)
    {
        CreateNewPersonalCanvas(person);

        if (sprite != null)
        {
            Image expressionImage = person.personalCanvas.transform.GetChild(1).GetComponent<Image>();
            expressionImage.sprite = sprite;
            person.personalCanvas.transform.GetChild(1).gameObject.SetActive(true);
        }
        
        StartCoroutine(ExecuteAfterDelay(duration, () => DestroyPersonalCanvas(person)));
    }
    
    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        direction.y = 0; 

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime);
        }
    }
    
    public static System.Collections.IEnumerator ExecuteAfterDelay(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }
}