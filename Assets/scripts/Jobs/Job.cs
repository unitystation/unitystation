using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Job : MonoBehaviour {

    // See modules/jobs/job_types/job.dm
    public string Title = "NOPE";
    public int TotalPositions = 0;
    public int CurrentPositions = 0;
    public DepartmentFlag DepartmentFlag = DepartmentFlag.NONE;
    public List<GameObject> Outfit = new List<GameObject>(); 
}
