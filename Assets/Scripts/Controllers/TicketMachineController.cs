using UnityEngine;

public class TicketMachineController : StationFacility
{
    void Start()
    {
        FacilityManager.Instance.RegisterFacility(this);
    }

    protected override void DeliverService(Passenger passenger)
    {
        PassengerManager.Instance.ReceiveTicket(passenger);
        int price = 0;

        if (passenger.assignedTrainService != null && passenger.assignedTrainService.trainData != null)
        {
            price = passenger.assignedTrainService.trainData.costPerRide;
            
            if (WorldSpacePromptCoordinator.Instance != null && price > 0)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + price, transform.position + Vector3.up * 7f, Color.darkGreen);
            }
        }
    }
}