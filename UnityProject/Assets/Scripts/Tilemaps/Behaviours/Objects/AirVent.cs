using Atmospherics;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Objects
{
	public class AirVent : NetworkBehaviour
	{
		public float MinimumPressure = 101.325f;

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
				AtmosUtils.SetAir(metaNode);
				_subsystemManager.UpdateAt(metaNode.Position);
			}
		}
	}
}