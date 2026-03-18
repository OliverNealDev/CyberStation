using UnityEngine;

public class CoolingObeliskController : StationFacility
{
    public int usePrice = 2;
    
    void Start()
    {
        FacilityManager.Instance.RegisterFacility(this);
    }

    protected override void DeliverService(Passenger passenger)
    {
        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Cold, passenger);
        int price = usePrice;
        EconomyManager.Instance.AddMoney(price);
        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$" + price, transform.position + Vector3.up * 7f, Color.darkGreen);
        }
    }
}