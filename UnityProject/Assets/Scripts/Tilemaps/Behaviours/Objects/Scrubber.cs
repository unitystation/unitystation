using Atmospherics;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	public class Scrubber : MonoBehaviour
	{
		private const float MaximumPressure = 101525f;

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
			lock (metaNode)
			{
				if (metaNode.Atmos.Pressure > MaximumPressure)
				{
					metaNode.Atmos = GasMixUtils.Space;
					systemManager.UpdateAt(metaNode.Position);
				}
			}
		}
	}
}