using UnityEngine;

public class DrinkVendingMachineController : StationFacility
{
    public int drinkPrice = 1;
    
    protected override void Start()
    {
        base.Start();
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
