using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;

namespace Systems.Spawns
{
	public class SpawnPoint : NetworkStartPosition
	{
		private static readonly Dictionary<JobType, SpawnPointCategory> categoryByJob = new Dictionary<JobType, SpawnPointCategory>
	{
		{ JobType.CAPTAIN, SpawnPointCategory.Captain },
		{ JobType.HOP, SpawnPointCategory.HeadOfPersonnel },

		{ JobType.AI, SpawnPointCategory.AI },

		{ JobType.QUARTERMASTER, SpawnPointCategory.Quartermaster },
		{ JobType.CARGOTECH, SpawnPointCategory.CargoTechnician },
		{ JobType.MINER, SpawnPointCategory.ShaftMiner },

		{ JobType.CMO, SpawnPointCategory.ChiefMedicalOfficer },
		{ JobType.DOCTOR, SpawnPointCategory.Medical },
		{ JobType.VIROLOGIST, SpawnPointCategory.Medical }, // TODO: should be virology point.
		{ JobType.PARAMEDIC, SpawnPointCategory.Medical },
		{ JobType.PSYCHIATRIST, SpawnPointCategory.Medical },
		{ JobType.CHEMIST, SpawnPointCategory.Chemist },
		{ JobType.GENETICIST, SpawnPointCategory.Medical },
		{ JobType.MEDSCI, SpawnPointCategory.Medical },

		{ JobType.RD, SpawnPointCategory.ResearchDirector },
		{ JobType.SCIENTIST, SpawnPointCategory.Scientist },
		{ JobType.ROBOTICIST, SpawnPointCategory.Roboticist },

		{ JobType.HOS, SpawnPointCategory.HeadOfSecurity },
		{ JobType.SECURITY_OFFICER, SpawnPointCategory.SecurityOfficer },
		{ JobType.WARDEN, SpawnPointCategory.Warden },
		{ JobType.DETECTIVE, SpawnPointCategory.Detective },
		{ JobType.LAWYER, SpawnPointCategory.Lawyer },

		{ JobType.CHIEF_ENGINEER, SpawnPointCategory.ChiefEngineer },
		{ JobType.ENGINEER, SpawnPointCategory.StationEngineer },
		{ JobType.ENGSEC, SpawnPointCategory.StationEngineer },
		{ JobType.ATMOSTECH, SpawnPointCategory.AtmosphericTechnician },

		{ JobType.ASSISTANT, SpawnPointCategory.Assistant },
		{ JobType.JANITOR, SpawnPointCategory.Janitor },
		{ JobType.CLOWN, SpawnPointCategory.Clown },
		{ JobType.MIME, SpawnPointCategory.Mime },
		{ JobType.CURATOR, SpawnPointCategory.Curator },
		{ JobType.COOK, SpawnPointCategory.Cook },
		{ JobType.BARTENDER, SpawnPointCategory.Bartender },
		{ JobType.BOTANIST, SpawnPointCategory.Botanist },
		{ JobType.CHAPLAIN, SpawnPointCategory.Chaplain },

		{ JobType.CENTCOMM_COMMANDER, SpawnPointCategory.CentCommCommander },
		{ JobType.CENTCOMM_OFFICER, SpawnPointCategory.CentComm },
		{ JobType.CENTCOMM_INTERN, SpawnPointCategory.CentComm },
		{ JobType.ERT_COMMANDER, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.ERT_SECURITY, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.ERT_MEDIC, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.ERT_ENGINEER, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.ERT_CHAPLAIN, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.ERT_JANITOR, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.ERT_CLOWN, SpawnPointCategory.EmergencyResponseTeam },
		{ JobType.DEATHSQUAD, SpawnPointCategory.DeathSquad },

		{ JobType.FUGITIVE, SpawnPointCategory.MaintSpawns },

		{ JobType.SYNDICATE, SpawnPointCategory.NuclearOperative },
		{ JobType.WIZARD, SpawnPointCategory.WizardFederation },
	};

		[SerializeField, FormerlySerializedAs("Department")]
		private SpawnPointCategory category = default;

		public static IEnumerable<Transform> GetPointsForCategory(SpawnPointCategory category)
		{
			return NetworkManager.startPositions.Select(x => x.transform)
					.Where(x => x.GetComponent<SpawnPoint>().category == category);
		}

		public static Transform GetRandomPointForLateSpawn()
		{
			return GetPointsForCategory(SpawnPointCategory.LateJoin).ToList().PickRandom();
		}

		public static Transform GetRandomPointForJob(JobType job)
		{
			Transform point;
			if (categoryByJob.ContainsKey(job) &&
					(point = GetPointsForCategory(categoryByJob[job]).ToList().PickRandom()) != null)
			{
				return point;
			}

			// Default to arrivals if there is no mapped spawn point for this job!
			// Will still return null if there is no arrivals spawn points set (and people will just not spawn!).
			return GetRandomPointForLateSpawn();
		}
	}

	public enum SpawnPointCategory
	{
		Assistant,
		Medical,
		StationEngineer,
		SecurityOfficer,
		Scientist,
		ResearchDirector,
		Roboticist,
		AI,
		ChiefEngineer,
		AtmosphericTechnician,
		Lawyer,
		Warden,
		Detective,
		HeadOfSecurity,
		Cook,
		Bartender,
		Curator,
		NuclearOperative,
		Captain,
		HeadOfPersonnel,
		CargoTechnician,
		Quartermaster,
		Janitor,
		ShaftMiner,
		Entertainers,
		ChiefMedicalOfficer,
		Chemist,
		Botanist,
		Chaplain,
		Clown,
		Mime,
		LateJoin,
		GhostTeleportSites,
		CentCommCommander,
		CentComm,
		DeathSquad,
		EmergencyResponseTeam,
		MaintSpawns,
		WizardFederation,
		SpaceExterior,
	}
}
