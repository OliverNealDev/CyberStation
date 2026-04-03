using UnityEngine;
using UnityEngine.EventSystems;

public class HoverShowChild : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject childObject;

    private void Start()
    {
        if (childObject != null)
        {
            childObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (childObject != null)
        {
            childObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (childObject != null)
        {
            childObject.SetActive(false);
        }
    }
}
