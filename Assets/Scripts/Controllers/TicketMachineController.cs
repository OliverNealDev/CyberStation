using System.Collections.Generic;
using UnityEngine;

public class TicketMachineController : MonoBehaviour
{
    public Passenger currentPassenger;
    public List<Passenger> PassengersOnWay = new List<Passenger>();
    
    public TicketMachineStates currentTicketMachineState;
    public enum TicketMachineStates
    {
        Idle,
        ProcessingTicket,
    }

    void Start()
    {
        currentTicketMachineState = TicketMachineStates.Idle;
        TicketMachineManager.Instance.RegisterTicketMachine(this);
    }

    public void AssignPassengerOnWay(Passenger passenger)
    {
        if (!PassengersOnWay.Contains(passenger))
        {
            PassengersOnWay.Add(passenger);
        }
    }
    
    public void RemovePassengerOnWay(Passenger passenger)
    {
        if (PassengersOnWay.Contains(passenger))
        {
            PassengersOnWay.Remove(passenger);
        }
    }
    
    public void ProcessTicketRequest(Passenger passenger)
    {
        if (currentTicketMachineState == TicketMachineStates.Idle)
        {
            currentTicketMachineState = TicketMachineStates.ProcessingTicket;
            currentPassenger = passenger;
            Invoke(nameof(FinishProcessingTicket), 3f); 
        }
    }
    
    private void FinishProcessingTicket()
    {
        if (currentPassenger != null)
        {
            PassengerManager.Instance.ReceiveTicket(currentPassenger);
            currentPassenger = null;
        }
        
        currentTicketMachineState = TicketMachineStates.Idle;
    }
    
    public void OnDestroy()
    {
        if (TicketMachineManager.Instance != null)
        {
            TicketMachineManager.Instance.DeregisterTicketMachine(this);
        }
    }
}