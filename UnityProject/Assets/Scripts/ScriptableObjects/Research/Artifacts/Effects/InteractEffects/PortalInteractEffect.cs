using UnityEngine;
using Objects.Research;
using Systems.Explosions;
using Items;
using Systems.Scenes;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "PortalInteractEffect", menuName = "ScriptableObjects/Systems/Artifacts/PortalInteractEffect")]
	public class PortalInteractEffect : InteractEffectBase
	{
		[SerializeField] private GameObject portalPrefab = null;

		[SerializeField] private int maxTeleportDistance = 20;
		[SerializeField] private int portalDuration = 30;

		private const int SPARK_COUNT = 3;

		private Portal entrancePortal;

		private Portal exitPortal;

		protected override void BareHandEffect(HandApply interaction) //Sets beacon to random location
		{
			GameObject artifact = interaction.TargetObject;
			UniversalObjectPhysics artifactPhysics = artifact.GetComponent<UniversalObjectPhysics>();

			Chat.AddExamineMsg(interaction.Performer, $"A crackling unstable portal forms beside the artifact!");

			var worldPosEntrance = artifactPhysics.OfficialPosition.RoundToInt();

			Vector3 worldPosExit = worldPosEntrance;
			int x = Random.Range(-maxTeleportDistance, maxTeleportDistance);
			int y = Random.Range(-maxTeleportDistance, maxTeleportDistance);

			worldPosEntrance += new Vector3Int(Random.Range(-1,1), Random.Range(-1, 1), Random.Range(-1, 1));
			worldPosExit += new Vector3(x, y, 0);

			SpawnPortals(worldPosEntrance, worldPosExit);
		}

		protected override void WrongEffect(HandApply interaction)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, wrongItemMessages.PickRandom());
		}

		protected override void CorrectEffect(HandApply interaction) //Sets portal to random beacon
		{
			GameObject artifact = interaction.TargetObject;
			UniversalObjectPhysics artifactPhysics = artifact.GetComponent<UniversalObjectPhysics>();

			Chat.AddExamineMsg(interaction.Performer, $"A portal forms beside the artifact!");

			var worldPosEntrance = artifactPhysics.OfficialPosition.RoundToInt();
			worldPosEntrance += new Vector3Int(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));

			var beacon = TrackingBeacon.GetAllBeaconOfType(TrackingBeacon.TrackingBeaconTypes.All).PickRandom();

			SpawnPortals(worldPosEntrance, beacon.ObjectBehaviour.OfficialPosition);
		}

		private void SpawnPortals(Vector3 worldPosEntrance, Vector3 worldPosExit)
		{
			if (entrancePortal != null) entrancePortal.PortalDeath();
			if (exitPortal != null) exitPortal.PortalDeath();

			//Spark three times entrance
			for (int i = 0; i < SPARK_COUNT; i++)
			{
				SparkUtil.TrySpark(worldPosEntrance, expose: false);
			}

			//Spark three times exit
			for (int i = 0; i < SPARK_COUNT; i++)
			{
				SparkUtil.TrySpark(worldPosExit, expose: false);
			}


			bool CanTeleport = true;
			foreach(TeleportInhibitor inhib in TeleportInhibitor.Inhibitors)
			{
				var inhibPosition = inhib.GetComponent<UniversalObjectPhysics>().OfficialPosition.RoundToInt();
				if(Vector3.Distance(inhibPosition, worldPosEntrance) <= inhib.Range)
				{
					SparkUtil.TrySpark(worldPosEntrance, expose: false);
					CanTeleport = false;
				}

				if(Vector3.Distance(inhibPosition, worldPosExit) <= inhib.Range)
				{
					SparkUtil.TrySpark(worldPosExit, expose: false);
					CanTeleport = false;
				}
			}

			if (CanTeleport == false) return;
			
			var entrance = Spawn.ServerPrefab(portalPrefab, worldPosEntrance);
			if (entrance.Successful == false) return;

			entrancePortal = entrance.GameObject.GetComponent<Portal>();

			var exit = Spawn.ServerPrefab(portalPrefab, worldPosExit);
			if (exit.Successful == false) return;

			exitPortal = exit.GameObject.GetComponent<Portal>();

			entrancePortal.SetNewPortal(exitPortal, portalDuration);
			exitPortal.SetNewPortal(entrancePortal, portalDuration);

			exitPortal.SetOrange();
		}
	}
}

