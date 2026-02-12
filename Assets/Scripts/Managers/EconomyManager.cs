using System;
using UnityEngine;

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
}
