using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;
    
    public static event Action<int> OnMoneyChanged;
    public static event Action<int, Sprite> OnExpenseRecorded;

    public int money = 2050;
    private readonly Dictionary<StaffMember, float> staffBillingDueTimes = new Dictionary<StaffMember, float>();
    private readonly Dictionary<TrainService, float> trainBillingDueTimes = new Dictionary<TrainService, float>();
    private const float RecurringBillingInterval = 60f;
    
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        OnMoneyChanged?.Invoke(money);
    }

    void Update()
    {
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            AddMoney(money);
        }

        UpdateRecurringCharges();
    }
    
    public void AddMoney(int amount)
    {
        money += amount; 
        OnMoneyChanged?.Invoke(money);
    }
    
    public void SpendMoney(int amount)
    {
        SpendMoney(amount, null, false);
    }

    public void SpendMoney(int amount, Sprite icon, bool showBill)
    {
        money -= amount;
        OnMoneyChanged?.Invoke(money);

        if (showBill && amount > 0)
        {
            OnExpenseRecorded?.Invoke(amount, icon);
            SoundEffectController.Play(SoundEffectId.BillCharged);
        }
    }
    
    public void RefundTicket(Passenger passenger)
    {
        if (!passenger.hasTicket || passenger.isTicketEvader) return; // No refund for evaders or those without tickets

        int refundAmount = passenger.assignedTrainService.trainData.costPerRide;
        SpendMoney(refundAmount);
    }

    private void UpdateRecurringCharges()
    {
        float now = Time.time;
        UpdateStaffRecurringCharges(now);
        UpdateTrainRecurringCharges(now);
    }

    private void UpdateStaffRecurringCharges(float now)
    {
        if (StaffManager.Instance == null) return;

        Dictionary<StaffMember, int> activeStaffCounts = new Dictionary<StaffMember, int>();
        for (int i = 0; i < StaffManager.Instance.hiredStaff.Count; i++)
        {
            Staff staff = StaffManager.Instance.hiredStaff[i];
            if (staff == null || staff.staffType == null) continue;

            if (!activeStaffCounts.ContainsKey(staff.staffType))
            {
                activeStaffCounts[staff.staffType] = 0;
            }

            activeStaffCounts[staff.staffType]++;
        }

        List<StaffMember> trackedStaffTypes = new List<StaffMember>(staffBillingDueTimes.Keys);
        for (int i = 0; i < trackedStaffTypes.Count; i++)
        {
            if (!activeStaffCounts.ContainsKey(trackedStaffTypes[i]))
            {
                staffBillingDueTimes.Remove(trackedStaffTypes[i]);
            }
        }

        foreach (var kvp in activeStaffCounts)
        {
            StaffMember staffType = kvp.Key;
            int staffCount = kvp.Value;

            if (!staffBillingDueTimes.ContainsKey(staffType))
            {
                staffBillingDueTimes[staffType] = now + RecurringBillingInterval;
                continue;
            }

            float dueTime = staffBillingDueTimes[staffType];
            while (now >= dueTime)
            {
                int amount = staffType.salaryPerMinute * staffCount;
                if (amount > 0)
                {
                    SpendMoney(amount, staffType.GetIcon(), true);
                }

                dueTime += RecurringBillingInterval;
            }

            staffBillingDueTimes[staffType] = dueTime;
        }
    }

    private void UpdateTrainRecurringCharges(float now)
    {
        if (TrainManager.Instance == null) return;

        HashSet<TrainService> activeServices = new HashSet<TrainService>();
        for (int i = 0; i < TrainManager.Instance.activeTrainServices.Count; i++)
        {
            TrainService service = TrainManager.Instance.activeTrainServices[i];
            if (service?.trainData == null) continue;
            activeServices.Add(service);
        }

        List<TrainService> trackedServices = new List<TrainService>(trainBillingDueTimes.Keys);
        for (int i = 0; i < trackedServices.Count; i++)
        {
            if (!activeServices.Contains(trackedServices[i]))
            {
                trainBillingDueTimes.Remove(trackedServices[i]);
            }
        }

        foreach (TrainService service in activeServices)
        {
            if (!trainBillingDueTimes.ContainsKey(service))
            {
                trainBillingDueTimes[service] = now + RecurringBillingInterval;
                continue;
            }

            float dueTime = trainBillingDueTimes[service];
            while (now >= dueTime)
            {
                int amount = service.trainData.costPerMinute;
                if (amount > 0)
                {
                    SpendMoney(amount, service.trainData.GetIcon(), true);
                }

                dueTime += RecurringBillingInterval;
            }

            trainBillingDueTimes[service] = dueTime;
        }
    }
}
