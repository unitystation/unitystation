using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class SpawnPoint : NetworkStartPosition
{
    private static readonly Dictionary<JobDepartment, JobType[]> DepartmentJobs
        = new Dictionary<JobDepartment, JobType[]>
        {
		{ JobDepartment.TheGrayTide, new [] { JobType.ASSISTANT, JobType.CLOWN, JobType.MIME} },
		    { JobDepartment.Captain, new [] { JobType.CAPTAIN} },
			{ JobDepartment.HoP, new [] { JobType.HOP} },
		{ JobDepartment.Cargo, new [] { JobType.HOP, JobType.CARGOTECH, JobType.QUARTERMASTER} },
            { JobDepartment.Personnel, new [] {JobType.CURATOR, JobType.COOK, JobType.CHAPLAIN, JobType.BOTANIST, JobType.BARTENDER, JobType.MINER, JobType.JANITOR} },
            { JobDepartment.Medical, new [] {JobType.GENETICIST, JobType.CHEMIST, JobType.CMO, JobType.DOCTOR, JobType.MEDSCI, JobType.VIROLOGIST} },
            { JobDepartment.Research, new [] {JobType.RD, JobType.SCIENTIST} },
            { JobDepartment.Security, new [] {JobType.DETECTIVE, JobType.HOS, JobType.LAWYER, JobType.SECURITY_OFFICER, JobType.WARDEN} },
            { JobDepartment.Engineering, new [] {JobType.ROBOTICIST, JobType.AI, JobType.ATMOSTECH, JobType.CHIEF_ENGINEER, JobType.ENGINEER, JobType.ENGSEC} },
		    { JobDepartment.Syndicate, new [] {JobType.SYNDICATE} }
   
	};

    public IEnumerable<JobType> JobRestrictions => DepartmentJobs[Department]; 

    public JobDepartment Department;

	public static JobDepartment GetJobDepartment(JobType job)
	{
		return DepartmentJobs.FirstOrDefault(x => x.Value.Contains(job)).Key;
	}
}