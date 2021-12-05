using System;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class RoomGasSetter : MonoBehaviour
	{
		[SerializeField]
		private GasMixesSO gasMixToSpawn = null;
		public GasMix GasMixToSpawn => gasMixToSpawn.BaseGasMix;

		private RegisterTile registerTile;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			var metaDataNode = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);

			if (metaDataNode == null || metaDataNode.RoomNumber == -1) return;

			if (GameManager.Instance.CurrentRoundState != RoundState.Started) return;

			if (gasMixToSpawn == null)
			{
				Logger.LogError($"Gas mix was null on {gameObject.ExpensiveName()}");
				return;
			}

			//This only happens after round has started, for spawned in room gas setters during the round
			registerTile.Matrix.OrNull()?.GetComponentInParent<AtmosSystem>().OrNull()?.SetRoomGas(metaDataNode.RoomNumber, GasMixToSpawn);
		}

		public void SetUp()
		{
			if (gasMixToSpawn == null)
			{
				Logger.LogError($"Gas mix was null on {gameObject.ExpensiveName()}");
				return;
			}

			var metaDataNode = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);

			if (metaDataNode == null || metaDataNode.RoomNumber == -1) return;

			//Set room before round start, do it during the atmos init
			registerTile.Matrix.OrNull()?.GetComponentInParent<AtmosSystem>().OrNull()?.AddToList(metaDataNode.RoomNumber, this);
		}
	}
}
