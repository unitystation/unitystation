using Atmospherics;
using Mirror;
using System;
using UnityEngine;

namespace Objects.GasContainer
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
		
		#region Lifecycle

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			UpdateGasMix();
			integrity.OnApplyDamage.AddListener(OnServerDamage);
		}

		private void OnDisable()
		{
			SetVentClosed();
			integrity.OnApplyDamage.RemoveListener(OnServerDamage);
		}

		private void OnServerDamage(DamageInfo info)
		{
			if (integrity.integrity - info.Damage <= 0)
			{
				SetVentClosed();
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
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " exploded!", WorldPosition.To2Int());

			ServerContainerExplode?.Invoke();
			// Disable this script, gameObject has no valid container now.
			enabled = false;
		}

		private void ReleaseContentsInstantly()
		{
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(WorldPosition, true).MetaDataLayer;
			MetaDataNode node = metaDataLayer.Get(LocalPosition, false);
			
			node.GasMix += GasMix;
			metaDataLayer.UpdateSystemsAt(LocalPosition, SystemType.AtmosSystem);
		}

		public void SetVent(bool isOpen)
		{
			if (isOpen)
			{
				SetVentOpen();
			}
			else
			{
				SetVentClosed();
			}
		}

		private void SetVentOpen()
		{
			IsVenting = true;
			UpdateManager.Add(CallbackType.UPDATE, UpdateVenting);
		}

		private void SetVentClosed()
		{
			IsVenting = false;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateVenting);
		}

		private void UpdateVenting()
		{
			if (isServer)
			{
				VentContents();
			}
		}

		[Server]
		private void VentContents()
		{
			var metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position), true).MetaDataLayer;

			Vector3Int localPosition = transform.localPosition.RoundToInt();
			MetaDataNode node = metaDataLayer.Get(localPosition, false);

			float deltaPressure = Mathf.Min(GasMix.Pressure, ReleasePressure) - node.GasMix.Pressure;

			if (deltaPressure > 0)
			{
				float ratio = deltaPressure / GasMix.Pressure * Time.deltaTime;

				node.GasMix += GasMix * ratio;

				GasMix *= (1 - ratio);

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
			GasMix = GasMix.FromTemperature(Gases, Temperature, Volume);
		}
	}
}
