using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Staff : Person
{
    public int salaryPerMinute; // Set from the scriptable object
    public StaffMember staffType;
    
    /* Security Models
    public List<GameObject> securityBodyModels = new List<GameObject>();
    public List<GameObject> securityHairModels = new List<GameObject>();
    public List<GameObject> securityHeadModels = new List<GameObject>();
    
    public List<GameObject> janitorBodyModels = new List<GameObject>();
    public List<GameObject> janitorHairModels = new List<GameObject>();
    public List<GameObject> janitorHeadModels = new List<GameObject>();*/
    
    private PersonVisualsData securityVisualsData;
    private PersonVisualsData janitorVisualsData;

    private GameObject bodyModel;
    private GameObject hairModel;
    private GameObject headModel;
    
    private void Start()
    {
        InvokeRepeating("GetPaid", 60f, 60f); // Pay every 60 seconds

        securityVisualsData = GlobalPersonVisuals.Instance.securityVisualsData;
        janitorVisualsData = GlobalPersonVisuals.Instance.janitorVisualsData;
        
        SpawnStaffModels();
    }

    void SpawnStaffModels()
    {
        switch (this)
        {
            case SecurityGuard:
                bodyModel = securityVisualsData.GetRandomBodyModel();
                hairModel = securityVisualsData.GetRandomHairModel();
                headModel = securityVisualsData.GetRandomHeadModel();
                break;
            case Janitor:
                bodyModel = janitorVisualsData.GetRandomBodyModel();
                hairModel = janitorVisualsData.GetRandomHairModel();
                headModel = janitorVisualsData.GetRandomHeadModel();
                break;
        }

        GameObject bodyInstance = Instantiate(bodyModel, transform);
        GameObject hairInstance = Instantiate(hairModel, transform);
        GameObject headInstance = Instantiate(headModel, transform);
        
        Material skinMaterial = GlobalPersonVisuals.Instance.GetRandomSkinMaterial();
        
        headInstance.transform.GetChild(0).GetComponent<MeshRenderer>().material = skinMaterial;
    }

    protected override void OnTick(float tickLength)
    {
        PerformDuties();
    }

    public abstract void PerformDuties();
    
    private void GetPaid()
    {
        StaffManager.Instance.PaySalary(this);
    }
}