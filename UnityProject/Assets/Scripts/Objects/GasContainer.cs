using System;
using UnityEngine;
using Mirror;
using UnityEditor;
using NaughtyAttributes;
using Systems.Atmospherics;
using Systems.Explosions;

namespace Objects.Atmospherics
{
	public class GasContainer : NetworkBehaviour, IGasMixContainer, IServerSpawn, IServerInventoryMove
	{
		//max pressure for determining explosion effects - effects will be maximum at this contained pressure
		private static readonly float MAX_EXPLOSION_EFFECT_PRESSURE = 148517f;

		/// <summary>
		/// If the container is not <see cref="IsSealed"/>, then the container is assumed to be mixed with the tile,
		/// so the tile's gas mix is returned instead.
		/// </summary>
		public GasMix GasMix
		{
			get => IsSealed ? internalGasMix : TileMix;
			set => internalGasMix = value;
		}

		private GasMix internalGasMix;

		[InfoBox("Remember to right-click component header to validiate values.")]
		public GasMix StoredGasMix = new GasMix();

		public bool IsVenting { get; private set; } = false;

		/// <summary>
		/// If the gas container is not sealed, then the container is assumed to be mixed with the tile,
		/// so <see cref="GasMix"/> will return the tile's mix.
		/// </summary>
		public bool IsSealed { get; set; } = true;

		[Tooltip("This is the maximum moles the container should be able to contain without exploding.")]
		public float MaximumMoles = 0f;

		public float ReleasePressure = AtmosConstants.ONE_ATMOSPHERE;
		public float Volume;
		public float Temperature;

		private RegisterTile registerTile;
		private Integrity integrity;
		private Pickupable pickupable;

		public Action ServerContainerExplode;

		public float ServerInternalPressure => GasMix.Pressure;

		private GasMix TileMix => registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionServer).GasMix;

		private bool gasIsInitialised = false;

		[SyncVar]
		//Only updated and valid for canisters inside the players inventory!!!
		//How full the tank is
		private float fullPercentageClient = 0;

		public float FullPercentageClient => fullPercentageClient;

		//Valid serverside only
		public float FullPercentage => GasMix.Moles / MaximumMoles;

		[Tooltip("If true : Cargo will accept gases found within this container and can be sold.")]
		public bool CargoSealApproved = false;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			pickupable = GetComponent<Pickupable>();
			integrity = GetComponent<Integrity>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (!gasIsInitialised)
			{
				UpdateGasMix();
			}

			// Not all containers need integrity e.g. DisposalVirtualContainer
			if (integrity != null)
			{
				integrity.OnApplyDamage.AddListener(OnServerDamage);
			}
		}

		private void OnDisable()
		{
			if (integrity != null)
			{
				integrity.OnApplyDamage.RemoveListener(OnServerDamage);
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, InventoryUpdateLoop);
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

		public void EqualiseWithTile()
		{
			GasMix.MergeGasMix(TileMix);
			registerTile.Matrix.MetaDataLayer.UpdateSystemsAt(registerTile.LocalPosition, SystemType.AtmosSystem);
		}

		// Needed for the internals tank on the player UI, to know oxygen gas percentage
		public void OnInventoryMoveServer(InventoryMove info)
		{
			//If going to a player start loop
			if (info.ToPlayer != null && info.ToSlot != null)
			{
				UpdateManager.Add(InventoryUpdateLoop, 1f);
				return;
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, InventoryUpdateLoop);
		}

		//Serverside only update loop, runs every second, started when canister goes into players inventory
		private void InventoryUpdateLoop()
		{
			if (pickupable.ItemSlot == null) return;

			fullPercentageClient = FullPercentage;
		}

		[Server]
		private void ExplodeContainer()
		{
			var shakeIntensity = (byte) Mathf.Lerp(
				byte.MinValue, byte.MaxValue / 2, GasMix.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);
			var shakeDistance = Mathf.Lerp(1, 64, GasMix.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);

			//release all of our gases at once when destroyed
			ReleaseContentsInstantly();

			ExplosionUtils.PlaySoundAndShake(registerTile.WorldPositionServer, shakeIntensity, (int) shakeDistance);
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " exploded!", gameObject);

			ServerContainerExplode?.Invoke();
			// Disable this script, gameObject has no valid container now.
			enabled = false;
		}

		public void ReleaseContentsInstantly()
		{
			MetaDataLayer metaDataLayer = registerTile.Matrix.MetaDataLayer;
			MetaDataNode node = metaDataLayer.Get(registerTile.LocalPositionServer, false);

			GasMix.TransferGas(node.GasMix, GasMix, GasMix.Moles);
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
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

				var List = AtmosUtils.CopyGasArray(GasMix.GasData);

				for (int i = List.List.Count - 1; i >= 0; i--)
				{
					var gas = GasMix.GasesArray[i];
					StoredGasMix.GasData.SetMoles(gas.GasSO, gas.Moles);
				}

				List.Pool();



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