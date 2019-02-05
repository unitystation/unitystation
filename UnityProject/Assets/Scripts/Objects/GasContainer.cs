using System.Collections.Generic;
using Atmospherics;
using Boo.Lang;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace Objects
{
	public class GasContainer : NetworkBehaviour, IGasMixContainer
	{
		public GasMix GasMix { get; set; }

		public bool Opened;
		public float ReleasePressure = 101.325f;

		// Keeping a copy of these values for initialization and the editor
		public float Volume;
		public float Temperature;
		public float[] Gases = new float[Gas.Count];

		public override void OnStartServer()
		{
			UpdateGasMix();
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
				MetaDataLayer metaDataLayer = MatrixManager.AtPoint(Vector3Int.RoundToInt(transform.position)).MetaDataLayer;

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