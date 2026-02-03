using System.Collections.Generic;
using UnityEngine;

public class TicketMachineManager : MonoBehaviour
{
    public static TicketMachineManager Instance;
    
    public List<TicketMachineController> AllTicketMachines = new List<TicketMachineController>();

    void Awake()
    {
        Instance = this;
    }
    
    public List<TicketMachineController> AvailableTicketMachines
    {
        get
        {
            List<TicketMachineController> availableMachines = new List<TicketMachineController>();
            foreach (var machine in AllTicketMachines)
            {
                if (machine.currentTicketMachineState == TicketMachineController.TicketMachineStates.Idle)
                {
                    availableMachines.Add(machine);
                }
            }
            return availableMachines;
        }
    }
    
    public TicketMachineController leastOccupiedTicketMachine
    {
        get
        {
            TicketMachineController leastOccupied = null;
            int minPassengers = int.MaxValue;

            foreach (var machine in AllTicketMachines)
            {
                int passengerCount = machine.PassengersOnWay.Count;
                if (passengerCount < minPassengers)
                {
                    minPassengers = passengerCount;
                    leastOccupied = machine;
                }
            }

            return leastOccupied;
        }
    }
    
    public void RegisterTicketMachine(TicketMachineController ticketMachine)
    {
        if (!AllTicketMachines.Contains(ticketMachine))
        {
            AllTicketMachines.Add(ticketMachine);
        }
    }
    
    public void DeregisterTicketMachine(TicketMachineController ticketMachine)
    {
        if (AllTicketMachines.Contains(ticketMachine))
        {
            AllTicketMachines.Remove(ticketMachine);
        }
    }
}
