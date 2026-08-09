using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;
    
    public static event Action<int> OnMoneyChanged;
    public static event Action<int> OnIncomePerMinuteChanged;
    public static event Action<int, Sprite> OnExpenseRecorded;

    public int money = 2050;
    private readonly Dictionary<StaffMember, float> staffBillingDueTimes = new Dictionary<StaffMember, float>();
    private float nextTrainBillingDueTime = -1f;
    private const float RecurringBillingInterval = 60f;
    private const float IncomeAverageWindowSeconds = 60f;

    private readonly Queue<MoneyChangeSample> recentMoneyChanges = new Queue<MoneyChangeSample>();
    private int recentMoneyWindowTotal;
    private int currentIncomePerMinute;

    public int CurrentIncomePerMinute => currentIncomePerMinute;

    private struct MoneyChangeSample
    {
        public float timestamp;
        public int delta;
    }
    
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        OnMoneyChanged?.Invoke(money);
        OnIncomePerMinuteChanged?.Invoke(currentIncomePerMinute);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            AddMoney(money, false);
        }

        UpdateRecurringCharges();
        UpdateIncomeAverage(Time.time);
    }
    
    public void AddMoney(int amount, bool includeInIncomeAverage = true)
    {
        ChangeMoney(amount, includeInIncomeAverage);
    }
    
    public void SpendMoney(int amount, bool includeInIncomeAverage = false)
    {
        SpendMoney(amount, null, false, includeInIncomeAverage);
    }

    public void SpendMoney(int amount, Sprite icon, bool showBill, bool includeInIncomeAverage = false)
    {
        ChangeMoney(-amount, includeInIncomeAverage);

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

    public void LoadMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        recentMoneyChanges.Clear();
        recentMoneyWindowTotal = 0;
        currentIncomePerMinute = 0;

        OnMoneyChanged?.Invoke(money);
        OnIncomePerMinuteChanged?.Invoke(currentIncomePerMinute);
    }

    private void UpdateRecurringCharges()
    {
        float now = Time.time;
        UpdateStaffRecurringCharges(now);
        UpdateTrainRecurringCharges(now);
    }

    private void ChangeMoney(int delta, bool includeInIncomeAverage)
    {
        money += delta;
        if (includeInIncomeAverage)
        {
            RecordMoneyChange(delta);
        }

        OnMoneyChanged?.Invoke(money);
    }

    private void RecordMoneyChange(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        recentMoneyChanges.Enqueue(new MoneyChangeSample
        {
            timestamp = Time.time,
            delta = delta
        });

        recentMoneyWindowTotal += delta;
        UpdateIncomeAverage(Time.time);
    }

    private void UpdateIncomeAverage(float now)
    {
        float cutoff = now - IncomeAverageWindowSeconds;
        while (recentMoneyChanges.Count > 0 && recentMoneyChanges.Peek().timestamp < cutoff)
        {
            MoneyChangeSample expiredChange = recentMoneyChanges.Dequeue();
            recentMoneyWindowTotal -= expiredChange.delta;
        }

        int updatedIncomePerMinute = Mathf.RoundToInt(recentMoneyWindowTotal * (60f / IncomeAverageWindowSeconds));
        if (updatedIncomePerMinute == currentIncomePerMinute)
        {
            return;
        }

        currentIncomePerMinute = updatedIncomePerMinute;
        OnIncomePerMinuteChanged?.Invoke(currentIncomePerMinute);
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
                    SpendMoney(amount, staffType.GetIcon(), true, true);
                }

                dueTime += RecurringBillingInterval;
            }

            staffBillingDueTimes[staffType] = dueTime;
        }
    }

    private void UpdateTrainRecurringCharges(float now)
    {
        if (TrainManager.Instance == null) return;

        int activeTrainCostPerMinute = 0;
        Sprite trainBillIcon = null;
        for (int i = 0; i < TrainManager.Instance.activeTrainServices.Count; i++)
        {
            TrainService service = TrainManager.Instance.activeTrainServices[i];
            if (service?.trainData == null) continue;

            activeTrainCostPerMinute += Mathf.Max(0, service.trainData.costPerMinute);
            if (trainBillIcon == null)
            {
                trainBillIcon = service.trainData.icon != null
                    ? service.trainData.icon
                    : service.trainData.GetIcon();
            }
        }

        if (activeTrainCostPerMinute <= 0)
        {
            nextTrainBillingDueTime = -1f;
            return;
        }

        if (nextTrainBillingDueTime < 0f)
        {
            nextTrainBillingDueTime = now + RecurringBillingInterval;
            return;
        }

        int elapsedBillingIntervals = 0;
        while (now >= nextTrainBillingDueTime)
        {
            elapsedBillingIntervals++;
            nextTrainBillingDueTime += RecurringBillingInterval;
        }

        if (elapsedBillingIntervals > 0)
        {
            SpendMoney(activeTrainCostPerMinute * elapsedBillingIntervals, trainBillIcon, true, true);
        }
    }
}
