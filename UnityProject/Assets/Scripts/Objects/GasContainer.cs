using System;
using UnityEngine;
using Mirror;
using Systems.Atmospherics;
using Systems.Explosions;

namespace Objects.Atmospherics
{
	[RequireComponent(typeof(Integrity))]
	public class GasContainer : NetworkBehaviour, IGasMixContainer, IServerSpawn
	{
		//max pressure for determining explosion effects - effects will be maximum at this contained pressure
		private static readonly float MAX_EXPLOSION_EFFECT_PRESSURE = 148517f;

		public GasMix GasMix { get; set; }

		public bool IsVenting { get; private set; } = false;

		[Tooltip("This is the maximum moles the container should be able to contain without exploding.")]
		public float MaximumMoles = 0f;

		public float ReleasePressure = 101.325f;

		// Keeping a copy of these values for initialization and the editor
		public float Volume;

		//hide these values as they're defined in GasContainerEditor.cs
		[HideInInspector] public float Temperature;
		[HideInInspector] public float[] Gases = new float[Gas.Count];

		private Integrity integrity;

		public Action ServerContainerExplode;

		public float ServerInternalPressure => GasMix.Pressure;
		private Vector3Int WorldPosition => gameObject.RegisterTile().WorldPosition;
		private Vector3Int LocalPosition => gameObject.RegisterTile().LocalPosition;

		private bool gasIsInitialised = false;

		#region Lifecycle

		private void Awake()
		{
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

				foreach (Gas gas in Gas.All)
				{
					Gases[gas] = GasMix.Gases[gas];
				}
			}
		}

		public void UpdateGasMix()
		{
			gasIsInitialised = true;
			GasMix = GasMix.FromTemperature(Gases, Temperature, Volume);
		}
	}
}
