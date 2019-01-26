using Atmospherics;
using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	public class Scrubber : MonoBehaviour
	{
		public float MaximumPressure = 101.025f;

		private SubsystemManager _subsystemManager;
		private MetaDataNode metaNode;

		private void Awake()
		{
			_subsystemManager = GetComponentInParent<SubsystemManager>();
		}

		private void Start()
		{
			MetaDataLayer metaDataLayer = GetComponentInParent<MetaDataLayer>();
			metaNode = metaDataLayer.Get(transform.localPosition.RoundToInt());
		}

		private void Update()
		{
			if (metaNode.Atmos.Pressure > MaximumPressure)
			{
				metaNode.Atmos = GasMixes.Space;
				_subsystemManager.UpdateAt(metaNode.Position);
			}
		}
	}
}