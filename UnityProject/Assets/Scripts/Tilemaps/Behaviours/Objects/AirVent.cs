using Atmospherics;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Objects
{
	public class AirVent : NetworkBehaviour
	{
		public float MinimumPressure = 101.325f;

		private SubsystemManager subsystemManager;
		private MetaDataNode metaNode;

		private void Awake()
		{
			subsystemManager = GetComponentInParent<SubsystemManager>();
		}

		void OnEnable()
		{
			UpdateManager.Instance.Add(UpdateMe);
		}

		void OnDisable()
		{
			if (UpdateManager.Instance != null)
			{
				UpdateManager.Instance.Remove(UpdateMe);
			}
		}

		private void Start()
		{
			MetaDataLayer metaDataLayer = GetComponentInParent<MetaDataLayer>();
			metaNode = metaDataLayer.Get(transform.localPosition.RoundToInt());
		}

		void UpdateMe()
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
				metaNode.Atmos = GasMixes.Air;
				subsystemManager.UpdateAt(metaNode.Position);
			}
		}
	}
}