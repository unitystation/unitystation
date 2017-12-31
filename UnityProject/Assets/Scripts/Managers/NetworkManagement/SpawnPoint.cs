using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class SpawnPoint : NetworkStartPosition
{
    private static readonly Dictionary<Department, JobType[]> DepartmentJobs
        = new Dictionary<Department, JobType[]>
        {
            { Department.TheGrayTide, new [] { JobType.ASSISTANT, JobType.BARTENDER, JobType.BOTANIST, JobType.CAPTAIN, JobType.CHAPLAIN, JobType.CLOWN, JobType.COOK, JobType.CURATOR, JobType.JANITOR} },
            { Department.Personnel, new [] {JobType.AI, JobType.CARGOTECH, JobType.HOP, JobType.MIME, JobType.MINER, JobType.QUARTERMASTER} },
            { Department.Medical, new [] {JobType.CHEMIST, JobType.CMO, JobType.DOCTOR, JobType.MEDSCI} },
            { Department.Research, new [] {JobType.GENETICIST, JobType.RD, JobType.ROBOTICIST, JobType.SCIENTIST, JobType.VIROLOGIST} },
            { Department.Security, new [] {JobType.DETECTIVE, JobType.HOS, JobType.LAWYER, JobType.SECURITY_OFFICER, JobType.WARDEN} },
            { Department.Engineering, new [] {JobType.ATMOSTECH, JobType.CHIEF_ENGINEER, JobType.ENGINEER, JobType.ENGSEC} },
        };

    public IEnumerable<JobType> JobRestrictions => DepartmentJobs[Department]; 

    public Department Department;

	public static Department GetJobDepartment(JobType job)
	{
		return DepartmentJobs.FirstOrDefault(x => x.Value.Contains(job)).Key;
	}
}