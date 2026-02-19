using System.Collections.Generic;
using UnityEngine;

public class CoffeeVendingMachineManager : MonoBehaviour
{
    public static CoffeeVendingMachineManager Instance;
    
    public List<CoffeeVendingMachineController> AllCoffeeVendingMachines = new List<CoffeeVendingMachineController>();

    public int coffeePrice;

    void Awake()
    {
        Instance = this;
    }
    
    public List<CoffeeVendingMachineController> AvailableCoffeeVendingMachines
    {
        get
        {
            List<CoffeeVendingMachineController> availableMachines = new List<CoffeeVendingMachineController>();
            foreach (var machine in AllCoffeeVendingMachines)
            {
                if (machine.state == CoffeeVendingMachineController.MachineState.Idle)
                {
                    availableMachines.Add(machine);
                }
            }
            return availableMachines;
        }
    }
    
    public CoffeeVendingMachineController leastOccupiedCoffeeVendingMachine
    {
        get
        {
            CoffeeVendingMachineController leastOccupied = null;
            int minPassengers = int.MaxValue;

            foreach (var machine in AllCoffeeVendingMachines)
            {
                int passengerCount = machine.PeopleOnWay.Count;
                if (passengerCount < minPassengers)
                {
                    minPassengers = passengerCount;
                    leastOccupied = machine;
                }
            }

            return leastOccupied;
        }
    }
    
    public void RegisterCoffeeVendingMachine(CoffeeVendingMachineController coffeeMachine)
    {
        if (!AllCoffeeVendingMachines.Contains(coffeeMachine))
        {
            AllCoffeeVendingMachines.Add(coffeeMachine);
        }
    }
    
    public void DeregisterCoffeeVendingMachine(CoffeeVendingMachineController coffeeMachine)
    {
        if (AllCoffeeVendingMachines.Contains(coffeeMachine))
        {
            AllCoffeeVendingMachines.Remove(coffeeMachine);
        }
    }
}
