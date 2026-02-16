using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrainServicesPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject rowPrefab;
    public Transform container;
    
    [Header("Settings")]
    public int maxRowsToShow = 5;
    
    private Dictionary<string, GameObject> activeRows = new Dictionary<string, GameObject>();

    void Start()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.OnTrainDepartedUI += HandleTrainDeparture;
        }
    }

    void OnDestroy()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.OnTrainDepartedUI -= HandleTrainDeparture;
        }
    }

    void Update()
    {
        if (TrainManager.Instance == null) return;

        List<ScheduledArrival> schedule = TrainManager.Instance.masterSchedule;
        
        int count = 0;
        foreach (var arrival in schedule)
        {
            if (count >= maxRowsToShow) break;

            if (!activeRows.ContainsKey(arrival.uiID))
            {
                SpawnRow(arrival);
            }
            
            if (activeRows.ContainsKey(arrival.uiID))
            {
                activeRows[arrival.uiID].transform.SetSiblingIndex(count);
            }

            count++;
        }
        
        List<string> toRemove = new List<string>();
        foreach(var kvp in activeRows)
        {
            bool foundInTop5 = false;
            for(int i=0; i < Mathf.Min(schedule.Count, maxRowsToShow); i++)
            {
                if(schedule[i].uiID == kvp.Key) foundInTop5 = true;
            }

            if(!foundInTop5 && kvp.Value.transform.parent == container) 
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach(var key in toRemove)
        {
            if(activeRows[key] != null) Destroy(activeRows[key]);
            activeRows.Remove(key);
        }
    }

    void SpawnRow(ScheduledArrival data)
    {
        GameObject newRow = Instantiate(rowPrefab, container);
        TrainRowUI script = newRow.GetComponent<TrainRowUI>();
        if (script)
        {
            script.Setup(data);
        }
        activeRows.Add(data.uiID, newRow);
    }
    
    void HandleTrainDeparture(ScheduledArrival arrival)
    {
        if (activeRows.ContainsKey(arrival.uiID))
        {
            GameObject row = activeRows[arrival.uiID];
            activeRows.Remove(arrival.uiID); 

            StartCoroutine(AnimateRowDepartureSmooth(row));
        }
    }

    IEnumerator AnimateRowDepartureSmooth(GameObject row)
    {
        RectTransform rowRect = row.GetComponent<RectTransform>();
        float originalHeight = rowRect.rect.height;

        GameObject ghost = new GameObject("LayoutGhost", typeof(RectTransform), typeof(LayoutElement));
        ghost.transform.SetParent(container, false);
        ghost.transform.SetSiblingIndex(row.transform.GetSiblingIndex());
        
        LayoutElement ghostLE = ghost.GetComponent<LayoutElement>();
        ghostLE.minHeight = originalHeight;
        ghostLE.preferredHeight = originalHeight;

        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());

        Vector3 startWorldPos = row.transform.position;
        row.transform.SetParent(container.parent, true);
        row.transform.position = startWorldPos;

        float timer = 0f;
        float duration = 0.6f;
        CanvasGroup cg = row.GetComponent<CanvasGroup>();
        if (cg == null) cg = row.AddComponent<CanvasGroup>();

        Vector3 initialPos = row.transform.position;
        Vector3 targetPos = initialPos + new Vector3(Screen.width * 0.6f, 0, 0);

        while (timer < duration)
        {
            float t = timer / duration;
            float smoothT = t * t * (3f - 2f * t);

            row.transform.position = Vector3.Lerp(initialPos, targetPos, smoothT);
            cg.alpha = 1f - smoothT;

            if (t > 0.2f) 
            {
                float shrinkT = (t - 0.2f) / 0.8f; 
                float curHeight = Mathf.Lerp(originalHeight, 0, shrinkT);
                ghostLE.minHeight = curHeight;
                ghostLE.preferredHeight = curHeight;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(row);
        Destroy(ghost);
    }
}