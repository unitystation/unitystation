using Atmospherics;
using Mirror;
using UnityEngine;

namespace Objects
{
	[RequireComponent(typeof(Integrity))]
	public class GasContainer : NetworkBehaviour, IGasMixContainer
	{
		//max pressure for determining explosion effects - effects will be maximum at this contained pressure
		private static readonly float MAX_EXPLOSION_EFFECT_PRESSURE = 148517f;

		public GasMix GasMix { get; set; }

		public bool Opened;
		[Tooltip("This is the maximum moles the container should be able to contain without exploding.")]
		public float MaximumMoles = 0f;
		public float ReleasePressure = 101.325f;

		// Keeping a copy of these values for initialization and the editor
		public float Volume;

		//hide these values as they're defined in GasContainerEditor.cs
		[HideInInspector]
		public float Temperature;
		[HideInInspector]
		public float[] Gases = new float[Gas.Count];

		public float ServerInternalPressure => GasMix.Pressure;

		public override void OnStartServer()
		{
			UpdateGasMix();
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);

		}

		private void OnWillDestroyServer(DestructionInfo info)
		{
			var tileWorldPosition = gameObject.TileWorldPosition().To3Int();
			//release all of our gases at once when destroyed
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(tileWorldPosition, true).MetaDataLayer;
			Vector3Int position = transform.localPosition.RoundToInt();
			MetaDataNode node = metaDataLayer.Get(position, false);
			var shakeIntensity = (byte) Mathf.Lerp( byte.MinValue, byte.MaxValue / 2, GasMix.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);
			var shakeDistance = Mathf.Lerp(1, 64, GasMix.Pressure / MAX_EXPLOSION_EFFECT_PRESSURE);
			node.GasMix += GasMix;
			metaDataLayer.UpdateSystemsAt(position);
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " exploded!", gameObject.TileWorldPosition());

			Spawn.ServerPrefab("Metal", gameObject.TileWorldPosition().To3Int(), transform.parent, count: 2,
				scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);

			ExplosionUtils.PlaySoundAndShake(tileWorldPosition, shakeIntensity, (int) shakeDistance);
		}

		private void Update()
		{
			if (isServer)
			{
				CheckRelease();
			}
		}

		[Server]
		private void CheckRelease()
		{
			if (Opened)
			{
				MetaDataLayer metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position), true).MetaDataLayer;

				Vector3Int position = transform.localPosition.RoundToInt();
				MetaDataNode node = metaDataLayer.Get(position, false);

				float deltaPressure = Mathf.Min(GasMix.Pressure, ReleasePressure) - node.GasMix.Pressure;

				if (deltaPressure > 0)
				{
					float ratio = deltaPressure / GasMix.Pressure * Time.deltaTime;

					node.GasMix += GasMix * ratio;

					GasMix *= (1 - ratio);

					metaDataLayer.UpdateSystemsAt(position);

					Volume = GasMix.Volume;
					Temperature = GasMix.Temperature;

					foreach (Gas gas in Gas.All)
					{
						Gases[gas] = GasMix.Gases[gas];
					}
				}
			}
		}

		public void UpdateGasMix()
		{
			GasMix = GasMix.FromTemperature(Gases, Temperature, Volume);
		}
	}
}