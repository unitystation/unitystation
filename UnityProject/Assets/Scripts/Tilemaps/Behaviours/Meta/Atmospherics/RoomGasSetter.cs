using System;
using Logs;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class RoomGasSetter : MonoBehaviour, IServerSpawn
	{
		[SerializeField]
		private GasMixesSO gasMixToSpawn = null;
		public GasMix GasMixToSpawn => gasMixToSpawn.BaseGasMix;

		private RegisterTile registerTile;
		public RegisterTile RegisterTile => registerTile;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if(CustomNetworkManager.IsServer == false) return;

			var metaDataNode = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);

			if (metaDataNode == null) return;

			if (GameManager.Instance.CurrentRoundState != RoundState.Started) return;

			if (gasMixToSpawn == null)
			{
				Loggy.LogError($"Gas mix was null on {gameObject.ExpensiveName()}");
				return;
			}

			if (metaDataNode.RoomNumber != -1)
			{
				//This only happens after round has started, for spawned in room gas setters during the round
				registerTile.Matrix.OrNull()?.GetComponentInParent<AtmosSystem>().OrNull()?.SetRoomGas(metaDataNode.RoomNumber, GasMixToSpawn);
			}
			else if (metaDataNode.IsOccupied)
			{
				//Use ChangeGasMix to remove old gas overlays and add new overlays
				metaDataNode.ChangeGasMix(GasMix.NewGasMix(GasMixToSpawn));
			}

			_ = Despawn.ServerSingle(gameObject);
		}

		public void SetUp()
		{
			if (gasMixToSpawn == null)
			{
				Loggy.LogError($"Gas mix was null on {gameObject.ExpensiveName()}");
				return;
			}

			var metaDataNode = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);

			if (metaDataNode == null) return;

			if (metaDataNode.RoomNumber != -1)
			{
				//Set room before round start, do it during the atmos init
				registerTile.Matrix.OrNull()?.GetComponentInParent<AtmosSystem>().OrNull()?.AddToListRoom(metaDataNode.RoomNumber, this);
			}
			else
			{
				//Set occupied before round start, do it during the atmos init
				registerTile.Matrix.OrNull()?.GetComponentInParent<AtmosSystem>().OrNull()?.AddToListOccupied(registerTile.LocalPositionServer, this);
			}
		}
	}
}
