using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
using NaughtyAttributes;
using Systems.Atmospherics;
using Systems.Explosions;
using ScriptableObjects.Atmospherics;
using UI.Systems.Tooltips.HoverTooltips;

namespace Objects.Atmospherics
{
	public class GasContainer : NetworkBehaviour, IGasMixContainer, IServerSpawn, IServerInventoryMove, ICheckedInteractable<InventoryApply>, IHoverTooltip
	{
		//max pressure for determining explosion effects - effects will be maximum at this contained pressure
		private static readonly float MAX_EXPLOSION_EFFECT_PRESSURE = 148517f;

		public GasSO GasSO;

		/// <summary>
		/// If the container is not <see cref="IsSealed"/>, then the container is assumed to be mixed with the tile,
		/// so the tile's gas mix is returned instead.
		/// </summary>
		public GasMix GasMixLocal
		{
			get => IsSealed ? internalGasMix : TileMix;
			set
			{
				internalGasMix = value;
				OnContentsUpdate?.Invoke();
			}
		}

		public event Action OnContentsUpdate;

		private GasMix internalGasMix;

		[InfoBox("Remember to right-click component header to validiate values.")]
		public GasMix StoredGasMix = new GasMix();

		public bool IsVenting { get; private set; } = false;

		/// <summary>
		/// If the gas container is not sealed, then the container is assumed to be mixed with the tile,
		/// so <see cref="GasMixLocal"/> will return the tile's mix.
		/// </summary>
		public bool IsSealed { get; set; } = true;

		[Tooltip("This is the maximum moles the container should be able to contain without exploding.")]
		public float MaximumMoles = 0f;

		public float ReleasePressure = AtmosConstants.ONE_ATMOSPHERE;

		private RegisterTile registerTile;
		private Integrity integrity;
		private Pickupable pickupable;

		public Action ServerContainerExplode;

		public float ServerInternalPressure => GasMixLocal.Pressure;

		private GasMix TileMix => registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionServer).GasMixLocal;

		private bool gasIsInitialised = false;

		[SyncVar]
		//Only updated and valid for canisters inside the players inventory!!!
		//How full the tank is
		private float fullPercentageClient = 0;

		public float FullPercentageClient => fullPercentageClient;

		//Valid serverside only
		public float FullPercentage => GasMixLocal.Moles / MaximumMoles;

		[Tooltip("If true : Cargo will accept gases found within this container and can be sold.")]
		public bool CargoSealApproved = false;

		[SerializeField] private bool explodeOnTooMuchDamage = true;

		[SyncVar, SerializeField] private bool ignoreInternals;
		public bool IgnoreInternals => ignoreInternals;
		[SerializeField] public bool canToggleIgnoreInternals = true;

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

		public override void OnStartClient()
		{
			SyncIgnoreInternals(ignoreInternals, ignoreInternals);
			base.OnStartClient();
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
			if (integrity.integrity - info.Damage <= 0 && explodeOnTooMuchDamage)
			{
				ExplodeContainer();
				integrity.RestoreIntegrity(integrity.initialIntegrity);
			}
		}

		private void SyncIgnoreInternals(bool _oldValue, bool _newValue)
		{
			ignoreInternals = _newValue;
			//Dont bother with any ui stuff if we arent in the local players inventory
			if (isClient && pickupable != null && pickupable.ItemSlot != null && pickupable.ItemSlot.LocalUISlot != null)
			{
				pickupable.RefreshUISlotImage();
				UIManager.Instance.internalControls.InventoryChange();
			}
		}

		#endregion Lifecycle

		public void EqualiseWithTile()
		{
			GasMixLocal.MergeGasMix(TileMix);
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
		public void ExplodeContainer()
		{
			var shakeIntensity = (byte) Mathf.Lerp(
				byte.MinValue, byte.MaxValue / 2, GasMixLocal.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);
			var shakeDistance = Mathf.Lerp(1, 64, GasMixLocal.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);

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

			if (node.GasMixLocal == GasMixLocal)
			{
				node.GasMixLocal.MergeGasMix(GasMixLocal);
			}
			else
			{
				GasMix.TransferGas(node.GasMixLocal, GasMixLocal, GasMixLocal.Moles);
			}
			metaDataLayer.UpdateSystemsAt(registerTile.LocalPositionServer, SystemType.AtmosSystem);
		}

		[Server]
		public void VentContents()
		{
			var metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position), true).MetaDataLayer;

			Vector3Int localPosition = transform.localPosition.RoundToInt();
			MetaDataNode node = metaDataLayer.Get(localPosition, false);

			float deltaPressure = Mathf.Min(GasMixLocal.Pressure, ReleasePressure) - node.GasMixLocal.Pressure;

			if (deltaPressure > 0)
			{
				float ratio = deltaPressure * Time.deltaTime;

				GasMix.TransferGas(node.GasMixLocal, GasMixLocal, ratio);

				metaDataLayer.UpdateSystemsAt(localPosition, SystemType.AtmosSystem);

				var List = AtmosUtils.CopyGasArray(GasMixLocal.GasData);

				for (int i = List.List.Count - 1; i >= 0; i--)
				{
					var gas = GasMixLocal.GasesArray[i];
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
			StoredGasMix = GasMix.FromTemperatureAndPressure(StoredGasMix.GasData, StoredGasMix.Temperature, StoredGasMix.Pressure, StoredGasMix.Volume );
		}
#endif
		public void UpdateGasMix()
		{
			gasIsInitialised = true;
			GasMixLocal = GasMix.FromTemperature(StoredGasMix.GasData, StoredGasMix.Temperature, StoredGasMix.Volume);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (interaction.TargetObject != gameObject) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (canToggleIgnoreInternals == false) return false;
			return interaction.IsAltClick;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.IsAltClick && canToggleIgnoreInternals)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, ignoreInternals
					? "You open the canister's valve and connect it to your internals."
					: "You close the canister's valve and disconnect it from your internals.");
				SyncIgnoreInternals(ignoreInternals, !ignoreInternals);
			}
		}

		public string HoverTip()
		{
			if (pickupable != null && canToggleIgnoreInternals)
			{
				return $"A tank full of gas, its valve is {(IgnoreInternals ? "closed" : "open")} and it {(IgnoreInternals ? "won't" : "will")} be used by your internals";
			}
			return null;
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			if (pickupable != null && canToggleIgnoreInternals)
			{
				var list = new List<TextColor>
				{
					new() { Color = Color.green, Text = "Alt Click: Toggle usage of tank with internals" },
				};
				return list;
			}
			return null;
		}
	}
}
