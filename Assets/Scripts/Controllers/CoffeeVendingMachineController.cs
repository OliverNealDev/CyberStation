using UnityEngine;

public class CoffeeVendingMachineController : StationFacility
{
    public int drinkPrice = 1;
    
    void Start()
    {
        FacilityManager.Instance.RegisterFacility(this);
    }

    protected override void DeliverService(Passenger passenger)
    {
        PassengerManager.Instance.MeetNeedFromTarget(Passenger.NeedType.Energy, passenger);
        int price = drinkPrice;
        EconomyManager.Instance.AddMoney(price);
        if (WorldSpacePromptCoordinator.Instance != null)
        {
            WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                "+$" + price, transform.position + Vector3.up * 7f, Color.darkGreen);
        }
    }
}