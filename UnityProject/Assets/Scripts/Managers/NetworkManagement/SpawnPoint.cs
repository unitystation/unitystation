using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

public class SpawnPoint : NetworkStartPosition
{
    private static readonly Dictionary<JobDepartment, JobType[]> DepartmentJobs
        = new Dictionary<JobDepartment, JobType[]>
        {
	        {JobDepartment.TheGrayTide, new[] {JobType.ASSISTANT}},
	        {JobDepartment.Captain, new[] {JobType.CAPTAIN}},
	        {JobDepartment.HoP, new[] {JobType.HOP}},
	        {JobDepartment.Cargo, new[] {JobType.CARGOTECH, JobType.QUARTERMASTER}},
	        {JobDepartment.Mining, new[] {JobType.MINER}},
	        {JobDepartment.Medical, new[] {JobType.GENETICIST, JobType.DOCTOR, JobType.MEDSCI, JobType.VIROLOGIST}},
	        {JobDepartment.CMO, new[] {JobType.CMO}},
	        {JobDepartment.Chemist, new[] {JobType.CHEMIST}},
	        {JobDepartment.Research, new[] {JobType.RD, JobType.SCIENTIST, JobType.ROBOTICIST}},
	        {JobDepartment.Security, new[] {JobType.DETECTIVE, JobType.HOS, JobType.LAWYER, JobType.SECURITY_OFFICER, JobType.WARDEN}},
	        {JobDepartment.Engineering, new[] {JobType.AI, JobType.ATMOSTECH, JobType.CHIEF_ENGINEER, JobType.ENGINEER, JobType.ENGSEC}},
	        {JobDepartment.Janitor, new[] {JobType.JANITOR}},
	        {JobDepartment.Entertainers, new[] {JobType.CLOWN, JobType.MIME}},
	        {JobDepartment.Personnel, new[] {JobType.CURATOR, JobType.COOK, JobType.CHAPLAIN, JobType.BOTANIST, JobType.BARTENDER}},
	        {JobDepartment.Syndicate, new[] {JobType.SYNDICATE}},
	};

    public IEnumerable<JobType> JobRestrictions => DepartmentJobs[Department];

    public JobDepartment Department;

	public static JobDepartment GetJobDepartment(JobType job)
	{
		return DepartmentJobs.FirstOrDefault(x => x.Value.Contains(job)).Key;
	}
}