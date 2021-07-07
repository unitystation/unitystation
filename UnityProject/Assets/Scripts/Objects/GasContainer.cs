using System;
using UnityEngine;
using Mirror;
using Systems.Atmospherics;
using Systems.Explosions;
using UnityEditor;

namespace Objects.Atmospherics
{
	[RequireComponent(typeof(Integrity))]
	public class GasContainer : NetworkBehaviour, IGasMixContainer, IServerSpawn, IServerInventoryMove
	{
		//max pressure for determining explosion effects - effects will be maximum at this contained pressure
		private static readonly float MAX_EXPLOSION_EFFECT_PRESSURE = 148517f;

		public GasMix GasMix { get; set; }

		public GasMix StoredGasMix = new GasMix();

		public bool IsVenting { get; private set; } = false;

		[Tooltip("This is the maximum moles the container should be able to contain without exploding.")]
		public float MaximumMoles = 0f;

		public float ReleasePressure = 101.325f;
		public float Volume;
		public float Temperature;

		private Integrity integrity;

		public Action ServerContainerExplode;

		public float ServerInternalPressure => GasMix.Pressure;
		private Vector3Int WorldPosition => gameObject.RegisterTile().WorldPosition;
		private Vector3Int LocalPosition => gameObject.RegisterTile().LocalPosition;

		private bool gasIsInitialised = false;

		private Pickupable pickupable;

		[SyncVar]
		//Only updated and valid for canisters inside the players inventory!!!
		//How full the tank is
		private float fullPercentageClient = 0;
		public float FullPercentageClient => fullPercentageClient;

		//Valid serverside only
		public float FullPercentage => GasMix.Moles / MaximumMoles;

		#region Lifecycle

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			integrity = GetComponent<Integrity>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (!gasIsInitialised)
			{
				UpdateGasMix();
			}

			integrity.OnApplyDamage.AddListener(OnServerDamage);
		}

		private void OnDisable()
		{
			integrity.OnApplyDamage.RemoveListener(OnServerDamage);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
		}

		private void OnServerDamage(DamageInfo info)
		{
			if (integrity.integrity - info.Damage <= 0)
			{
				ExplodeContainer();
				integrity.RestoreIntegrity(integrity.initialIntegrity);
			}
		}

		#endregion Lifecycle

		// Needed for the internals tank on the player UI, to know oxygen gas percentage
		public void OnInventoryMoveServer(InventoryMove info)
		{
			//If going to a player start loop
			if (info.ToPlayer != null && info.ToSlot != null)
			{
				UpdateManager.Add(UpdateLoop, 1f);
				return;
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateLoop);
		}

		//Serverside only update loop, runs every second, started when canister goes into players inventory
		private void UpdateLoop()
		{
			if(pickupable.ItemSlot == null) return;

			fullPercentageClient = FullPercentage;
		}

		[Server]
		private void ExplodeContainer()
		{
			var shakeIntensity = (byte)Mathf.Lerp(
					byte.MinValue, byte.MaxValue / 2, GasMix.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);
			var shakeDistance = Mathf.Lerp(1, 64, GasMix.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);

			//release all of our gases at once when destroyed
			ReleaseContentsInstantly();

			ExplosionUtils.PlaySoundAndShake(WorldPosition, shakeIntensity, (int)shakeDistance);
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " exploded!", gameObject);

			ServerContainerExplode?.Invoke();
			// Disable this script, gameObject has no valid container now.
			enabled = false;
		}

		private void ReleaseContentsInstantly()
		{
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(WorldPosition, true).MetaDataLayer;
			MetaDataNode node = metaDataLayer.Get(LocalPosition, false);

			GasMix.TransferGas(node.GasMix, GasMix, GasMix.Moles);
			metaDataLayer.UpdateSystemsAt(LocalPosition, SystemType.AtmosSystem);
		}

		[Server]
		public void VentContents()
		{
			var metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position), true).MetaDataLayer;

			Vector3Int localPosition = transform.localPosition.RoundToInt();
			MetaDataNode node = metaDataLayer.Get(localPosition, false);

			float deltaPressure = Mathf.Min(GasMix.Pressure, ReleasePressure) - node.GasMix.Pressure;

			if (deltaPressure > 0)
			{
				float ratio = deltaPressure * Time.deltaTime;

				GasMix.TransferGas(node.GasMix, GasMix, ratio);

				metaDataLayer.UpdateSystemsAt(localPosition, SystemType.AtmosSystem);

				Volume = GasMix.Volume;
				Temperature = GasMix.Temperature;

				foreach (var gas in GasMix.GasesArray)
				{
					StoredGasMix.GasData.SetMoles(gas.GasSO, gas.Moles);
				}
			}
		}
#if UNITY_EDITOR

		[ContextMenu("Set Values for Gas")]
		private void Validate()
		{
			Undo.RecordObject(gameObject, "Gas Change");
			StoredGasMix = GasMix.FromTemperature(StoredGasMix.GasData, Temperature, Volume);
		}
#endif
		public void UpdateGasMix()
		{
			gasIsInitialised = true;
			GasMix = GasMix.FromTemperature(StoredGasMix.GasData, Temperature, Volume);
		}
	}
}
