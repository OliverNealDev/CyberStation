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
}
