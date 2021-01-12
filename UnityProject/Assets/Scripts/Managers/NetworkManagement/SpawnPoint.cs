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
		{ JobType.HOP, SpawnPointCategory.HoP },

		{ JobType.AI, SpawnPointCategory.AI },

		{ JobType.QUARTERMASTER, SpawnPointCategory.CargoHead },
		{ JobType.CARGOTECH, SpawnPointCategory.Cargo },
		{ JobType.MINER, SpawnPointCategory.Mining },

		{ JobType.CMO, SpawnPointCategory.CMO },
		{ JobType.DOCTOR, SpawnPointCategory.Medical },
		{ JobType.VIROLOGIST, SpawnPointCategory.Medical }, // TODO: should be virology point.
		{ JobType.PARAMEDIC, SpawnPointCategory.Medical },
		{ JobType.PSYCHIATRIST, SpawnPointCategory.Medical },
		{ JobType.CHEMIST, SpawnPointCategory.Chemist },
		{ JobType.GENETICIST, SpawnPointCategory.Medical },
		{ JobType.MEDSCI, SpawnPointCategory.Medical },

		{ JobType.RD, SpawnPointCategory.ResearchHead },
		{ JobType.SCIENTIST, SpawnPointCategory.Research },
		{ JobType.ROBOTICIST, SpawnPointCategory.Robotics },

		{ JobType.HOS, SpawnPointCategory.HOS },
		{ JobType.SECURITY_OFFICER, SpawnPointCategory.Security },
		{ JobType.WARDEN, SpawnPointCategory.Warden },
		{ JobType.DETECTIVE, SpawnPointCategory.Detective },
		{ JobType.LAWYER, SpawnPointCategory.Lawyer },

		{ JobType.CHIEF_ENGINEER, SpawnPointCategory.EngineeringHead },
		{ JobType.ENGINEER, SpawnPointCategory.Engineering },
		{ JobType.ENGSEC, SpawnPointCategory.Engineering },
		{ JobType.ATMOSTECH, SpawnPointCategory.Atmos },

		{ JobType.ASSISTANT, SpawnPointCategory.TheGrayTide },
		{ JobType.JANITOR, SpawnPointCategory.Janitor },
		{ JobType.CLOWN, SpawnPointCategory.Clown },
		{ JobType.MIME, SpawnPointCategory.Mime },
		{ JobType.CURATOR, SpawnPointCategory.Personnel },
		{ JobType.COOK, SpawnPointCategory.Kitchen },
		{ JobType.BARTENDER, SpawnPointCategory.Bar },
		{ JobType.BOTANIST, SpawnPointCategory.Botany },
		{ JobType.CHAPLAIN, SpawnPointCategory.Church },

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

		{ JobType.SYNDICATE, SpawnPointCategory.Syndicate },
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
		TheGrayTide,
		Medical,
		Engineering,
		Security,
		Research,
		ResearchHead,
		Robotics,
		AI,
		EngineeringHead,
		Atmos,
		Lawyer,
		Warden,
		Detective,
		HOS,
		Kitchen,
		Bar,
		Personnel,
		Syndicate,
		Captain,
		HoP,
		Cargo,
		CargoHead,
		Janitor,
		Mining,
		Entertainers,
		CMO,
		Chemist,
		Botany,
		Church,
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
