using UnityEngine;

public class RadiantObeliskController : StationFacility
{
    public int usePrice = 2;
    
    protected override void Start()
    {
        base.Start();
    }

    protected override void DeliverService(Passenger passenger)
    {
        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Energy, passenger);
        int price = usePrice;
        EconomyManager.Instance.AddMoney(price);
        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$" + price, transform.position + Vector3.up * 7f, Color.darkGreen);
        }
    }
}
