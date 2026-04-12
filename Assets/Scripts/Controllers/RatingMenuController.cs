using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

public class RatingMenuController : MonoBehaviour
{
    public Sprite starFilled;
    public Sprite starHalf;
    public Sprite starEmpty;
    
    private float tickRate = 20f; 
    private float tickInterval;
    private float tickTimer = 0f;
    
    public Transform stationRatingStarsContainer;
    public Transform cleanlinessRatingStarsContainer;
    public Transform crowdednessRatingStarsContainer;
    public Transform queueTimesRatingStarsContainer;
    public Transform passengerNeedsRatingStarsContainer;
    [FormerlySerializedAs("stationSizeRatingStarsContainer")]
    public Transform choiceRatingStarsContainer;
    [FormerlySerializedAs("trainSelectionRatingStarsContainer")]
    public Transform decorationRatingStarsContainer;
    [SerializeField] private TextMeshProUGUI throughputEfficiencyText;
    
    void Start()
    {
        tickInterval = 1f / tickRate;
        
        LoadRatings();
    }

    void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickInterval)
        {
            LoadRatings();
            tickTimer -= tickInterval;
        }
    }
    
    public void LoadRatings()
    {
        if (RatingManager.Instance == null) return;

        EnsureReferences();

        SetStars(stationRatingStarsContainer, RatingManager.Instance.stationRating);
        SetStars(cleanlinessRatingStarsContainer, RatingManager.Instance.cleanlinessRating);
        SetStars(crowdednessRatingStarsContainer, RatingManager.Instance.crowdednessRating);
        SetStars(queueTimesRatingStarsContainer, RatingManager.Instance.queueTimesRating);
        SetStars(passengerNeedsRatingStarsContainer, RatingManager.Instance.passengerNeedsRating);
        SetStars(choiceRatingStarsContainer, RatingManager.Instance.choiceRating);
        SetStars(decorationRatingStarsContainer, RatingManager.Instance.decorationRating);
        UpdateThroughputEfficiencyText();
    }
    
    void SetStars(Transform starsContainer, float rating)
    {
        if (starsContainer == null) return;

        float roundedRating = Mathf.Round(rating * 2f) / 2f;
        int fullStars = Mathf.FloorToInt(roundedRating);
        bool hasHalfStar = (roundedRating - fullStars) >= 0.5f;

        for (int i = 0; i < starsContainer.childCount; i++)
        {
            Image starImage = starsContainer.GetChild(i).GetComponent<Image>();

            if (i < fullStars)
            {
                starImage.sprite = starFilled;
            }
            else if (i == fullStars && hasHalfStar)
            {
                starImage.sprite = starHalf;
            }
            else
            {
                starImage.sprite = starEmpty;
            }
        }
    }

    private void EnsureReferences()
    {
        if (throughputEfficiencyText != null)
        {
            return;
        }

        TextMeshProUGUI[] textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < textComponents.Length; i++)
        {
            TextMeshProUGUI textComponent = textComponents[i];
            if (textComponent != null && textComponent.text.Contains("Passenger Throughput Efficiency"))
            {
                throughputEfficiencyText = textComponent;
                return;
            }
        }
    }

    private void UpdateThroughputEfficiencyText()
    {
        if (throughputEfficiencyText == null || RatingManager.Instance == null)
        {
            return;
        }

        float occupancy = PassengerSpawner.CalculateTargetOccupancy(RatingManager.Instance.stationRating);
        int occupancyPercent = Mathf.RoundToInt(occupancy * 100f);
        throughputEfficiencyText.text = $"Passenger Throughput Efficiency: {occupancyPercent}%";
    }
}
