using System.Collections.Generic;
using UnityEngine;

public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance;

    private static readonly Dictionary<FacilityType, string> FacilityBuildableResourcePaths = new Dictionary<FacilityType, string>
    {
        { FacilityType.TicketMachine, "BuildItems/TicketMachine" },
        { FacilityType.NutrientExtruder, "BuildItems/NutrientDispenser" },
        { FacilityType.SnackPrinter, "BuildItems/SnackPrinter" },
        { FacilityType.HydratingObelisk, "BuildItems/HydratingObelisk" },
        { FacilityType.BottleDispenser, "BuildItems/BottleDispenser" },
        { FacilityType.CleansingShower, "BuildItems/CleansingShower" },
        { FacilityType.PrivateLavatory, "BuildItems/PrivateLavatory" },
        { FacilityType.EnergyBottleDispenser, "BuildItems/EnergyBottleDispenser" }
    };

    private readonly Dictionary<FacilityType, ObjectBuildable> facilityBuildables = new Dictionary<FacilityType, ObjectBuildable>();

    private Dictionary<FacilityType, List<StationFacility>> facilitiesMap = new Dictionary<FacilityType, List<StationFacility>>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterFacility(StationFacility facility)
    {
        FacilityType type = facility.facilityType;

        if (!facilitiesMap.ContainsKey(type))
        {
            facilitiesMap[type] = new List<StationFacility>();
        }

        facilitiesMap[type].Add(facility);
    }

    public void DeregisterFacility(StationFacility facility)
    {
        if (facilitiesMap.ContainsKey(facility.facilityType))
        {
            facilitiesMap[facility.facilityType].Remove(facility);
        }
    }

    public StationFacility GetLeastOccupiedFacility(FacilityType type)
    {
        if (!facilitiesMap.ContainsKey(type) || facilitiesMap[type].Count == 0)
        {
            return null;
        }

        StationFacility leastOccupied = null;
        int minPassengers = int.MaxValue;

        foreach (var facility in facilitiesMap[type])
        {
            if (facility.PeopleOnWay.Count < minPassengers)
            {
                minPassengers = facility.PeopleOnWay.Count;
                leastOccupied = facility;
            }
        }

        return leastOccupied;
    }

    public List<StationFacility> GetFacilities(FacilityType type)
    {
        if (!facilitiesMap.TryGetValue(type, out List<StationFacility> facilities))
        {
            return null;
        }

        return facilities;
    }
    
    public bool HasFacility(FacilityType type)
    {
        return facilitiesMap.ContainsKey(type) && facilitiesMap[type].Count > 0;
    }

    public int GetTotalFacilityCount()
    {
        int count = 0;
        foreach (var list in facilitiesMap.Values)
        {
            count += list.Count;
        }
        return count;
    }

    public int GetTotalQueuedPassengers()
    {
        int count = 0;
        foreach (var list in facilitiesMap.Values)
        {
            foreach (var facility in list)
            {
                count += facility.PeopleOnWay.Count;
            }
        }
        return count;
    }

    public List<FacilityType> GetFacilitiesForNeed(Passenger.NeedType need)
    {
        List<FacilityType> validFacilities = new List<FacilityType>();

        switch (need)
        {
            case Passenger.NeedType.Ticket:
                validFacilities.Add(FacilityType.TicketMachine);
                break;
            case Passenger.NeedType.Hunger:
                validFacilities.Add(FacilityType.NutrientExtruder);
                validFacilities.Add(FacilityType.SnackPrinter);
                break;
            case Passenger.NeedType.Thirst:
                validFacilities.Add(FacilityType.HydratingObelisk);
                validFacilities.Add(FacilityType.BottleDispenser);
                break;
            case Passenger.NeedType.Energy:
                validFacilities.Add(FacilityType.EnergyBottleDispenser);
                validFacilities.Add(FacilityType.RestPad);
                break;
            case Passenger.NeedType.Hygiene:
                validFacilities.Add(FacilityType.MolecularScrubber);
                validFacilities.Add(FacilityType.CleansingShower);
                validFacilities.Add(FacilityType.PrivateLavatory);
                break;
        }

        return validFacilities;
    }

    public List<FacilityType> GetUnlockedFacilitiesForNeed(Passenger.NeedType need)
    {
        List<FacilityType> unlockedFacilities = new List<FacilityType>();
        List<FacilityType> possibleFacilities = GetFacilitiesForNeed(need);

        for (int i = 0; i < possibleFacilities.Count; i++)
        {
            FacilityType facilityType = possibleFacilities[i];
            if (IsFacilityTypeUnlocked(facilityType))
            {
                unlockedFacilities.Add(facilityType);
            }
        }

        return unlockedFacilities;
    }

    public bool IsFacilityTypeUnlocked(FacilityType facilityType)
    {
        ObjectBuildable buildable = GetBuildableForFacilityType(facilityType);
        if (buildable == null)
        {
            return false;
        }

        return ProgressionManager.Instance == null || ProgressionManager.Instance.IsUnlocked(buildable);
    }

    private ObjectBuildable GetBuildableForFacilityType(FacilityType facilityType)
    {
        if (facilityBuildables.TryGetValue(facilityType, out ObjectBuildable cachedBuildable))
        {
            return cachedBuildable;
        }

        if (!FacilityBuildableResourcePaths.TryGetValue(facilityType, out string resourcePath))
        {
            facilityBuildables[facilityType] = null;
            return null;
        }

        ObjectBuildable buildable = Resources.Load<ObjectBuildable>(resourcePath);
        facilityBuildables[facilityType] = buildable;
        return buildable;
    }
}
