using Atmospherics;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Objects
{
	public class AirVent : NetworkBehaviour
	{
		private const float MinimumPressure = 101325f;

		private SystemManager systemManager;
		private MetaDataNode metaNode;

		private void Awake()
		{
			systemManager = GetComponentInParent<SystemManager>();
		}

		private void Start()
		{
			MetaDataLayer metaDataLayer = GetComponentInParent<MetaDataLayer>();
			metaNode = metaDataLayer.Get(transform.localPosition.RoundToInt());
		}

		private void Update()
		{
			if (isServer)
			{
				CheckAtmos();
			}
		}

		[Server]
		private void CheckAtmos()
		{
			if (metaNode.Atmos.Pressure < MinimumPressure)
			{
				metaNode.Atmos = GasMixUtils.Air;
				systemManager.UpdateAt(metaNode.Position);
			}
		}
	}
}