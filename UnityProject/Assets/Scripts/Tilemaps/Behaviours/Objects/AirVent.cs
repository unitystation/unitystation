using Atmospherics;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	public class AirVent : MonoBehaviour
	{
		private const float MinimumPressure = 101325f;

		private AtmosControl atmosControl;
		private SystemManager systemManager;
		private MetaDataNode metaNode;

		private void Awake()
		{
			systemManager = GetComponentInParent<SystemManager>();
			atmosControl = GetComponentInParent<AtmosControl>();
		}

		private void Start()
		{
			MetaDataLayer metaDataLayer = GetComponentInParent<MetaDataLayer>();
			metaNode = metaDataLayer.Get(transform.localPosition.RoundToInt());
		}

		private void Update()
		{
			lock (metaNode)
			{
				if (metaNode.Atmos.Pressure < MinimumPressure)
				{
					metaNode.Atmos = GasMixUtils.Air;
					systemManager.UpdateAt(metaNode.Position);
				}
			}
		}
	}
}