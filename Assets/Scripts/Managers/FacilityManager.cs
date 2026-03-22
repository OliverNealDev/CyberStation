using System.Collections.Generic;
using UnityEngine;

public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance;

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
            case Passenger.NeedType.Hunger:
                validFacilities.Add(FacilityType.NutrientExtruder);
                validFacilities.Add(FacilityType.CaloricInjectionPort);
                break;
            case Passenger.NeedType.Thirst:
                validFacilities.Add(FacilityType.HydratingObelisk);
                break;
            case Passenger.NeedType.Energy:
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
}