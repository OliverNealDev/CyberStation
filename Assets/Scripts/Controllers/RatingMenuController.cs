using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingMenuController : MonoBehaviour
{
    public Sprite starFilled;
    public Sprite starEmpty;
    
    private float tickRate = 20f; 
    private float tickInterval;
    private float tickTimer = 0f;
    
    public Transform stationRatingStarsContainer;
    public Transform cleanlinessRatingStarsContainer;
    public Transform crowdednessRatingStarsContainer;
    public Transform queueTimesRatingStarsContainer;
    public Transform passengerNeedsRatingStarsContainer;
    public Transform trainSelectionRatingStarsContainer;
    public Transform stationSizeRatingStarsContainer;
    
    void Start()
    {
        tickInterval = 1f / tickRate;
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

        SetStars(stationRatingStarsContainer, RatingManager.Instance.stationRating);
        SetStars(cleanlinessRatingStarsContainer, RatingManager.Instance.cleanlinessRating);
        SetStars(crowdednessRatingStarsContainer, RatingManager.Instance.crowdednessRating);
        SetStars(queueTimesRatingStarsContainer, RatingManager.Instance.queueTimesRating);
        SetStars(passengerNeedsRatingStarsContainer, RatingManager.Instance.passengerNeedsRating);
        SetStars(trainSelectionRatingStarsContainer, RatingManager.Instance.trainSelectionRating);
        SetStars(stationSizeRatingStarsContainer, RatingManager.Instance.stationSizeRating);
    }
    
    void SetStars(Transform starsContainer, int rating)
    {
        int fullStars = rating;

        for (int i = 0; i < starsContainer.childCount; i++)
        {
            Image starImage = starsContainer.GetChild(i).GetComponent<Image>();

            if (i < fullStars)
            {
                starImage.sprite = starFilled;
            }
            else
            {
                starImage.sprite = starEmpty;
            }
        }
    }
}