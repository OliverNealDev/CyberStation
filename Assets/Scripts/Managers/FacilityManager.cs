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
}