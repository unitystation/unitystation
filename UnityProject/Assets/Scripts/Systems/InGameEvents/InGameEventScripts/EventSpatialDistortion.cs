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
			base.OnEventStart();
			var text = "Central Command Update:\n Incoming spatial distortions!! Be careful.";
			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			_ = SpawnPortals();

		}


		public async UniTask SpawnPortals()
		{

			var Positions = MatrixManager.MainStationMatrix.WorldMatrixCollisionBounds.Value.allPositionsWithin();

			int j = 2;

			var Ports = Positions.Count * PortalDensity;
			for (int i = 0; i < Ports; i++)
			{
				var World = Positions.PickRandom();

				if (MatrixManager.IsPassableAtAllMatricesOneTile(World, true) == false)
				{
					i--;
					continue;
				}

				var Space =  MatrixManager.IsNoGravityAt(World, true, MatrixManager.MainStationMatrix);
				if (Space)
				{
					i--;
					continue;
				}

				var Position =  World.ToLocal(MatrixManager.MainStationMatrix.Matrix).RoundToInt();
				if (MatrixManager.MainStationMatrix.MetaDataLayer.Get(Position, false, false).GasMixLocal.Pressure < 50)
				{
					if (RNG.RoleChance(SpaceSpawnedChance) == false)
					{
						i--;
						continue;
					}
				}

				ActivePortal.Add(Spawn.ServerPrefab(Portal, World).GameObject);

				j--;
				if (0 > j)
				{
					j = 2;
					await UniTask.Delay(100, false);
				}
			}
		}

		public override void OnEventEndTimed()
		{

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
					await UniTask.Delay(500, false);
				}
			}
			ActivePortal.Clear();
		}
	}
}