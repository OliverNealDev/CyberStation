using UnityEngine;

public class CoffeeVendingMachineController : QueuableObject
{
    public Person currentPerson;
    public enum MachineState { Idle, Processing }
    public MachineState state = MachineState.Idle;
    
    public override bool IsAvailable => state == MachineState.Idle;

    void Start()
    {
        CoffeeVendingMachineManager.Instance.RegisterCoffeeVendingMachine(this);
    }
    
    public override void ProcessInteraction(Person person)
    {
        if (state == MachineState.Idle)
        {
            state = MachineState.Processing;
            currentPerson = person;
            Invoke(nameof(FinishProcessing), 3f);
        }
    }

    private void FinishProcessing()
    {
        if (currentPerson != null)
        {
            Passenger passenger = (Passenger)currentPerson;
        
            PassengerManager.Instance.ReceiveCoffee(passenger);

            int coffeePrice = 0;

            coffeePrice = CoffeeVendingMachineManager.Instance.coffeePrice;
                
            if (WorldSpacePromptCoordinator.Instance != null)
            {
                WorldSpacePromptCoordinator.Instance.CreateWorldPrompt(
                    "+$" + coffeePrice, 
                    transform.position + Vector3.up * 7f,
                    Color.darkGreen);
            }

            currentPerson = null;
        }
        state = MachineState.Idle;
    }
}
