using System.Collections.Generic;
using System.Linq;
using Mirror;

public class SpawnPoint : NetworkStartPosition
{
	private static readonly Dictionary<JobDepartment, JobType[]> DepartmentJobs
		= new Dictionary<JobDepartment, JobType[]>
		{
			{JobDepartment.TheGrayTide, new[] {JobType.ASSISTANT}},
			{JobDepartment.Captain, new[] {JobType.CAPTAIN}},
			{JobDepartment.HoP, new[] {JobType.HOP}},
			{JobDepartment.Cargo, new[] {JobType.CARGOTECH}},
			{JobDepartment.CargoHead, new[] {JobType.QUARTERMASTER}},
			{JobDepartment.Mining, new[] {JobType.MINER}},
			{JobDepartment.Medical, new[] {JobType.GENETICIST, JobType.DOCTOR, JobType.MEDSCI, JobType.VIROLOGIST}},
			{JobDepartment.CMO, new[] {JobType.CMO}},
			{JobDepartment.Chemist, new[] {JobType.CHEMIST}},
			{JobDepartment.Research, new[] {JobType.SCIENTIST}},
			{JobDepartment.ResearchHead, new[] {JobType.RD}},
			{JobDepartment.Robotics, new[] {JobType.ROBOTICIST}},
			{JobDepartment.Security, new[] {JobType.SECURITY_OFFICER}},
			{JobDepartment.Warden, new[] {JobType.WARDEN}},
			{JobDepartment.Detective, new[] {JobType.DETECTIVE}},
			{JobDepartment.HOS, new[] {JobType.HOS}},
			{JobDepartment.Lawyer, new[] {JobType.LAWYER,}},
			{JobDepartment.Engineering, new[] {JobType.AI, JobType.ENGINEER, JobType.ENGSEC}},
			{JobDepartment.EngineeringHead, new[] {JobType.CHIEF_ENGINEER}},
			{JobDepartment.Atmos, new[] {JobType.ATMOSTECH}},
			{JobDepartment.Janitor, new[] {JobType.JANITOR}},
			{JobDepartment.Clown, new[] {JobType.CLOWN, }},
			{JobDepartment.Mime, new[] {JobType.MIME}},
			{JobDepartment.Personnel, new[] {JobType.CURATOR}},
			{JobDepartment.Kitchen, new[] {JobType.COOK}},
			{JobDepartment.Bar, new[] {JobType.BARTENDER}},
			{JobDepartment.Botany, new[] {JobType.BOTANIST}},
			{JobDepartment.Church, new[] {JobType.CHAPLAIN}},
			{JobDepartment.Syndicate, new[] {JobType.SYNDICATE}},
			{JobDepartment.CentCommCommander, new []{JobType.CENTCOMM_COMMANDER}},
			{JobDepartment.DeathSquad, new [] {JobType.DEATHSQUAD}},
			{JobDepartment.CentComm, new[] {JobType.CENTCOMM_OFFICER, JobType.CENTCOMM_INTERN}},
			{JobDepartment.EmergencyResponseTeam, new[] {JobType.ERT_COMMANDER, JobType.ERT_SECURITY, JobType.ERT_MEDIC, JobType.ERT_ENGINEER, JobType.ERT_CHAPLAIN, JobType.ERT_JANITOR, JobType.ERT_CLOWN}},
		};

	public IEnumerable<JobType> JobRestrictions =>
		DepartmentJobs.ContainsKey(Department) ? DepartmentJobs[Department] : new JobType[0];

	public JobDepartment Department;

	public static JobDepartment GetJobDepartment(JobType job)
	{
		return DepartmentJobs.FirstOrDefault(x => x.Value.Contains(job)).Key;
	}
}