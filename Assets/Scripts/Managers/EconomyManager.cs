using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    public int money = 1000;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UIController.Instance.UpdateMoneyDisplay(money);
    }

    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            AddMoney(money);
        }
    }
    
    public void AddMoney(int amount)
    {
        money += amount;
        UIController.Instance.UpdateMoneyDisplay(money);
    }
    
    public void SpendMoney(int amount)
    {
        money -= amount;
        UIController.Instance.UpdateMoneyDisplay(money);
    }
    
    public void RefundTicket(Passenger passenger)
    {
        if (!passenger.hasTicket || passenger.isTicketEvader) return; // No refund for evaders or those without tickets

        int refundAmount = passenger.assignedTrainService.trainData.costPerRide;
        AddMoney(refundAmount);
    }
}
