using System.Collections.Generic;
using Atmospherics;
using Boo.Lang;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace Objects
{
	public class GasContainer : NetworkBehaviour
	{
		public GasMix GasMix;

		public float Volume;
		public float Temperature;

		public bool Opened;
		public float ReleasePressure = 101.325f;

		public float[] Gases = new float[Gas.Count];

		public override void OnStartServer()
		{
			GasMix = GasMix.FromTemperature(Gases, Temperature, Volume);
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

				float deltaPressure = Mathf.Min(GasMix.Pressure, ReleasePressure) - node.Atmos.Pressure;

				if (deltaPressure > 0)
				{
					float ratio = deltaPressure / GasMix.Pressure * Time.deltaTime;

					node.Atmos += GasMix * ratio;

					GasMix *= (1 - ratio);

					metaDataLayer.UpdateSystemsAt(position);
				}
			}
		}
	}
}