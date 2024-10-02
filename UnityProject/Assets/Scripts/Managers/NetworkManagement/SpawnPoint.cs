using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using UnityEngine.Serialization;
using Mirror;
using SecureStuff;

namespace Systems.Spawns
{
	public class SpawnPoint : NetworkStartPosition
	{


		[VVNote(VVHighlight.SafeToModify100)]
		public SpawnPointCategory Category
		{
			get
			{
				return category;
			}
			set
			{
				category = value;
				if (SpriteHandler != null)
				{
					var Position = transform.position.RoundToInt();
					this.name = category.ToString() + " at " + Position.x + " " + Position.y;
					if (SpawnPointSpritesSingleton.Instance.Sprites.ContainsKey(category))
					{
						SpriteHandler.SetSpriteSO(SpawnPointSpritesSingleton.Instance.Sprites[category]);
					}
					else
					{
						Loggy.LogError("Was unable to find spawn Point Sprite for " + category);
					}

				}
			}
		}

		[SerializeField, FormerlySerializedAs("Department")]
		private SpawnPointCategory category = default;

		[SerializeField]
		public SpawnPointType type = SpawnPointType.Unlimited;

		[SerializeField]
		[Range(0, 10)]
		[Tooltip("Higher number means higher priority")]
		public int priority = 0;

		private bool used;

		public SpriteHandler SpriteHandler;


		[NaughtyAttributes.Button]
		public void UpdateData()
		{
			if (SpriteHandler != null)
			{
				var Position = transform.position.RoundToInt();
				this.name = category.ToString() + " at " + Position.x + " " + Position.y;
				if (SpawnPointSpritesSingleton.Instance.Sprites.ContainsKey(category))
				{
					SpriteHandler.SetSpriteSO(SpawnPointSpritesSingleton.Instance.Sprites[category]);
				}
				else
				{
					Loggy.LogError("Was unable to find spawn Point Sprite for " + category);
				}

			}
		}


		public static IEnumerable<Transform> GetPointsForCategory(SpawnPointCategory category)
		{
			return NetworkManager.startPositions.Select(x => x.transform)
				.Where(x => x.TryGetComponent<SpawnPoint>(out var spawn) && spawn.category == category && spawn.used == false);
		}

		public static Transform GetRandomPointForLateSpawn()
		{
			var points = GetPointsForCategory(SpawnPointCategory.LateJoin).ToList();
			if (points.Any() == false)
			{
				return NetworkManager.startPositions.PickRandom();
			}
			else
			{
				return points.PickRandom();
			}


		}

		public void OnEnable()
		{
			if (NetworkManager.startPositions.Contains(this.transform) ==false)
			{
				NetworkManager.RegisterStartPosition(transform);
			}
		}

		public void OnDisable()
		{
			if (NetworkManager.startPositions.Contains(this.transform))
			{
				NetworkManager.UnRegisterStartPosition(transform);
			}
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
			{ JobType.ANCIENT_ENGINEER, SpawnPointCategory.AncientEngineering },
			{ JobType.ASHWALKER, SpawnPointCategory.Ashwalker },
			{ JobType.THEWELDER, SpawnPointCategory.MaintSpawns },
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
		Ashwalker
	}

	public enum SpawnPointType
	{
		Unlimited,
		Once
	}
}
