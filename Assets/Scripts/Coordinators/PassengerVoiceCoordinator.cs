using System.Collections.Generic;
using UnityEngine;

// Lets passengers say what is wrong, out loud, above their heads. The speech bubble and
// dialogue data were already built into every passenger and never used; this decides
// who gets to speak, and keeps the chatter rare enough that each line still gets read.
public class PassengerVoiceCoordinator : MonoBehaviour
{
    private const float MinimumGapSeconds = 1.6f;
    private const float BubbleDuration = 3.2f;
    private const int MaxSimultaneousBubbles = 3;
    private const float UrgentLineChance = 0.35f;
    private const float AbandonLineChance = 0.5f;
    private const float HappyLineChance = 0.04f;
    private const float BubbleScale = 1.6f;

    private static readonly Color ComplaintColor = new Color(1f, 0.45f, 0.5f);
    private static readonly Color HappyColor = new Color(0.45f, 1f, 0.7f);
    private static readonly Color NeutralColor = Color.white;

    private static readonly Dictionary<Passenger.NeedType, string[]> UrgentLines = new Dictionary<Passenger.NeedType, string[]>
    {
        { Passenger.NeedType.Ticket, new[] { "Where do I buy a ticket?", "Is there a ticket machine anywhere?", "I'm going to miss my train..." } },
        { Passenger.NeedType.Hunger, new[] { "I'm starving.", "Nothing to eat in here?", "I'd kill for a nutrient cube." } },
        { Passenger.NeedType.Thirst, new[] { "I need a drink.", "So thirsty...", "Is there water anywhere?" } },
        { Passenger.NeedType.Energy, new[] { "I can barely keep my eyes open.", "I need an energy drink.", "Long shift. So tired." } },
        { Passenger.NeedType.Hygiene, new[] { "I really need a wash.", "Where are the facilities?", "I feel grim." } },
    };

    private static readonly string[] AbandonLines =
    {
        "Forget it.", "Worst station on the line.", "I'm writing a review.", "Unbelievable.", "Never again."
    };

    private static readonly string[] HappyLines =
    {
        "Now that's service.", "Nice station.", "Perfect.", "Love this place.", "Quick and easy."
    };

    private readonly List<float> bubbleEndTimes = new List<float>();
    private float nextAllowedTime;

    private void OnEnable()
    {
        PassengerManager.OnNeedBecameUrgent += HandleNeedUrgent;
        PassengerManager.OnNeedAbandoned += HandleNeedAbandoned;
        PassengerManager.OnNeedMet += HandleNeedMet;
        PassengerManager.OnEvaderCaught += HandleEvaderCaught;
        PassengerManager.OnServiceEndedForPassenger += HandleServiceEnded;
    }

    private void OnDisable()
    {
        PassengerManager.OnNeedBecameUrgent -= HandleNeedUrgent;
        PassengerManager.OnNeedAbandoned -= HandleNeedAbandoned;
        PassengerManager.OnNeedMet -= HandleNeedMet;
        PassengerManager.OnEvaderCaught -= HandleEvaderCaught;
        PassengerManager.OnServiceEndedForPassenger -= HandleServiceEnded;
    }

    private void HandleNeedUrgent(Passenger passenger, Passenger.NeedType needType)
    {
        if (Random.value < UrgentLineChance && UrgentLines.TryGetValue(needType, out string[] lines))
        {
            TrySpeak(passenger, Pick(lines), ComplaintColor);
        }
    }

    private void HandleNeedAbandoned(Passenger passenger, Passenger.NeedType needType)
    {
        if (Random.value < AbandonLineChance)
        {
            TrySpeak(passenger, Pick(AbandonLines), ComplaintColor);
        }
    }

    private void HandleNeedMet(Passenger passenger, Passenger.NeedType needType)
    {
        if (Random.value < HappyLineChance)
        {
            TrySpeak(passenger, Pick(HappyLines), HappyColor);
        }
    }

    private void HandleEvaderCaught(Passenger passenger)
    {
        TrySpeak(passenger, GetDialogueLine(passenger, DialogueType.CaughtBySecurity), NeutralColor);
    }

    private void HandleServiceEnded(Passenger passenger)
    {
        TrySpeak(passenger, GetDialogueLine(passenger, DialogueType.TrainServiceEnded), ComplaintColor);
    }

    private void TrySpeak(Passenger passenger, string line, Color color)
    {
        if (passenger == null || string.IsNullOrEmpty(line) || !passenger.gameObject.activeInHierarchy || UIController.IsUiHidden)
        {
            return;
        }

        float now = Time.time;
        bubbleEndTimes.RemoveAll(endTime => endTime <= now);
        if (now < nextAllowedTime || bubbleEndTimes.Count >= MaxSimultaneousBubbles)
        {
            return;
        }

        nextAllowedTime = now + MinimumGapSeconds;
        bubbleEndTimes.Add(now + BubbleDuration);
        passenger.Dialogue(passenger, line, color, BubbleDuration);

        // The bubble was sized for close-up shots; at the usual zoom it needs to be bigger to read.
        if (passenger.personalCanvas != null)
        {
            passenger.personalCanvas.transform.localScale *= BubbleScale;
        }
    }

    private static string GetDialogueLine(Passenger passenger, DialogueType type)
    {
        return passenger != null && passenger.dialogueData != null ? passenger.dialogueData.GetRandomLine(type) : null;
    }

    private static string Pick(string[] lines)
    {
        return lines[Random.Range(0, lines.Length)];
    }
}
