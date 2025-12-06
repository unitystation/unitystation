using System.Collections.Generic;
using Core.Utils;
using Cysharp.Threading.Tasks;
using Managers;
using Strings;
using UnityEngine;

namespace InGameEvents
{
	public class EventSpatialDistortion : EventScriptBase
	{
		public GameObject Portal;

		public static List<GameObject> ActivePortal = new List<GameObject>();

		public float PortalDensity = 0.005f;
		public float SpaceSpawnedChance = 0.20f;

		public override void OnEventStart()
		{
			var text = "Central Command Update:\n Incoming spatial distortions!! Be careful.";

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);



			_ = SpawnPortals();
		}


		public async UniTask SpawnPortals()
		{

			var Positions = MatrixManager.MainStationMatrix.WorldMatrixCollisionBounds.Value.allPositionsWithin();

			int j = 10;

			var Ports = Positions.Count * PortalDensity;
			for (int i = 0; i < Ports; i++)
			{
				var World = Positions.PickRandom();

				if (MatrixManager.IsPassableAtAllMatricesOneTile(World, true) == false) continue;

				var Space =  MatrixManager.IsSpaceAt(World, true);
				if (Space)
				{
					if (RNG.RoleChance(SpaceSpawnedChance) == false)
					{
						continue;
					}
				}

				ActivePortal.Add(Spawn.ServerPrefab(Portal, World).GameObject);

				j--;
				if (0 > j)
				{
					j = 10;
					UniTask.Delay(500, false);
				}
			}
		}

		public override void OnEventEndTimed()
		{
			foreach (var Portal in ActivePortal)
			{
				_ = Despawn.ServerSingle(Portal);
			}

			var text = "Central Command Update:\n Looks like the spatial distortions are over, Return to your normal activities.";

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);

			_ = RemovePortals();
		}

		public async UniTask RemovePortals()
		{
			int j = 10;
			for (int i = 0; i < ActivePortal.Count; i++)
			{

				if (ActivePortal[i] == null) continue;
				_ = Despawn.ServerSingle(ActivePortal[i]);

				j--;
				if (0 >j)
				{
					j = 10;
					UniTask.Delay(500, false);
				}
			}
			ActivePortal.Clear();
		}
	}
}