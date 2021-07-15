using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;

namespace Systems.Spawns
{
	public class SpawnPoint : NetworkStartPosition
	{
		[SerializeField, FormerlySerializedAs("Department")]
		private SpawnPointCategory category = default;

		[SerializeField]
		private SpawnPointType type = SpawnPointType.Unlimited;

		[SerializeField]
		[Range(0, 10)]
		[Tooltip("Higher number means higher priority")]
		private int priority = 0;

		private bool used;

		public static IEnumerable<Transform> GetPointsForCategory(SpawnPointCategory category)
		{
			return NetworkManager.startPositions.Select(x => x.transform)
				.Where(x => x.TryGetComponent<SpawnPoint>(out var spawn) && spawn.category == category && spawn.used == false);
		}

		public static Transform GetRandomPointForLateSpawn()
		{
			return GetPointsForCategory(SpawnPointCategory.LateJoin).ToList().PickRandom();
		}

		public static Transform GetRandomPointForJob(JobType job, bool isGhost = false)
		{
			if (categoryByJob.ContainsKey(job))
			{
				//Get all available points and order by priority, higher numbers picked first
				var points = GetPointsForCategory(categoryByJob[job]).OrderBy(x => x.GetComponent<SpawnPoint>().priority).ToList();

				if (points.Any() == false)
				{
					// Default to arrivals if there is no mapped spawn point for this job!
					// Will still return null if there is no arrivals spawn points set (and people will just not spawn!).
					return GetRandomPointForLateSpawn();
				}

				//Get last point as that should have biggest priority
				var last = points.Last();
				if (last != null && last.TryGetComponent<SpawnPoint>(out var spawn))
				{
					//If the priority isnt 0 then we use this one else choose random
					if (spawn.priority != 0)
					{
						//If this point is only allowed once then set it to used, dont allow ghosts to use up a spawn point
						if (spawn.type == SpawnPointType.Once && isGhost == false)
						{
							spawn.used = true;
						}

						return last;
					}

					//Pick random as all points will be 0
					last = points.PickRandom();

					//If this point is only allowed once then set it to used, dont allow ghosts to use up a spawn point
					if (spawn.type == SpawnPointType.Once && isGhost == false)
					{
						spawn.used = true;
					}

					return last;
				}
			}

			// Default to arrivals if there is no mapped spawn point for this job!
			// Will still return null if there is no arrivals spawn points set (and people will just not spawn!).
			return GetRandomPointForLateSpawn();
		}

		private const string DEFAULT_SPAWNPOINT_ICON = "Mapping/mapping_x2.png";
		private string iconName => iconNames.ContainsKey(category) ? iconNames[category] : DEFAULT_SPAWNPOINT_ICON;

		private void OnDrawGizmos()
		{
			Gizmos.DrawIcon(transform.position, iconName);
		}

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
			{ JobType.REDSHIELD_OFFICER, SpawnPointCategory.CentComm },
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
			{JobType.ANCIENT_ENGINEER, SpawnPointCategory.AncientEngineering},
		};

		private static readonly Dictionary<SpawnPointCategory, string> iconNames = new Dictionary<SpawnPointCategory, string>()
		{
			{SpawnPointCategory.Assistant, "Mapping/mapping_assistant.png"},
			{SpawnPointCategory.Medical, "Mapping/mapping_medical_doctor.png"},
			{SpawnPointCategory.StationEngineer, "Mapping/mapping_station_engineer.png"},
			{SpawnPointCategory.SecurityOfficer, "Mapping/mapping_security_officer.png"},
			{SpawnPointCategory.Scientist, "Mapping/mapping_scientist.png"},
			{SpawnPointCategory.ResearchDirector, "Mapping/mapping_research_director.png"},
			{SpawnPointCategory.Roboticist, "Mapping/mapping_roboticist.png"},
			{SpawnPointCategory.AI, "Mapping/mapping_AI.png"},
			{SpawnPointCategory.ChiefEngineer, "Mapping/mapping_chief_engineer.png"},
			{SpawnPointCategory.AtmosphericTechnician, "Mapping/mapping_atmospheric_technician.png"},
			{SpawnPointCategory.Lawyer, "Mapping/mapping_lawyer.png"},
			{SpawnPointCategory.Warden, "Mapping/mapping_warden.png"},
			{SpawnPointCategory.Detective, "Mapping/mapping_detective.png"},
			{SpawnPointCategory.HeadOfSecurity, "Mapping/mapping_head_of_security.png"},
			{SpawnPointCategory.Cook, "Mapping/mapping_cook.png"},
			{SpawnPointCategory.Bartender, "Mapping/mapping_bartender.png"},
			{SpawnPointCategory.Curator, "Mapping/mapping_curator.png"},
			{SpawnPointCategory.NuclearOperative, "Mapping/mapping_snukeop_spawn.png"},
			{SpawnPointCategory.Captain, "Mapping/mapping_captain.png"},
			{SpawnPointCategory.HeadOfPersonnel, "Mapping/mapping_head_of_personnel.png"},
			{SpawnPointCategory.CargoTechnician, "Mapping/mapping_cargo_technician.png"},
			{SpawnPointCategory.Quartermaster, "Mapping/mapping_quartermaster.png"},
			{SpawnPointCategory.Janitor, "Mapping/mapping_janitor.png"},
			{SpawnPointCategory.ShaftMiner, "Mapping/mapping_shaft_miner.png"},
			{SpawnPointCategory.ChiefMedicalOfficer, "Mapping/mapping_chief_medical_officer.png"},
			{SpawnPointCategory.Chemist, "Mapping/mapping_chemist.png"},
			{SpawnPointCategory.Botanist, "Mapping/mapping_botanist.png"},
			{SpawnPointCategory.Chaplain, "Mapping/mapping_chaplain.png"},
			{SpawnPointCategory.Clown, "Mapping/mapping_clown.png"},
			{SpawnPointCategory.Mime, "Mapping/mapping_mime.png"},
			{SpawnPointCategory.GhostTeleportSites, "Mapping/mapping_observer_start.png"},
			{SpawnPointCategory.CentCommCommander, "Mapping/mapping_ert_spawn.png"},
			{SpawnPointCategory.CentComm, "Mapping/mapping_ert_spawn.png"},
			{SpawnPointCategory.DeathSquad, "Mapping/mapping_ert_spawn.png"},
			{SpawnPointCategory.EmergencyResponseTeam, "Mapping/mapping_ert_spawn.png"},
			{SpawnPointCategory.MaintSpawns, "Mapping/mapping_mouse.png"},
			{SpawnPointCategory.WizardFederation, "Mapping/mapping_wiznerd_spawn.png"},
			{SpawnPointCategory.SpaceExterior, "Mapping/mapping_carp_spawn.png"},
			{SpawnPointCategory.AncientEngineering, "Mapping/mapping_station_engineer.png"}
		};

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
		AncientEngineering,
	}

	public enum SpawnPointType
	{
		Unlimited,
		Once
	}
}
