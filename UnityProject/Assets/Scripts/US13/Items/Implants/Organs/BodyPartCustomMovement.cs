using UnityEngine;
using US13.Core.Physics;
using US13.HealthV2.Living;
using US13.Player.MovementV2;
using Util;

namespace US13.Items.Implants.Organs
{

[System.Serializable]
public class BodyPartCustomMovement : BodyPartBehavior
{

	[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("RelatedHealth")] private uint _networkRelatedHealth;
		public uint RelatedHealth
		{
			get => _networkRelatedHealth;
			set
			{
				if (System.Collections.Generic.EqualityComparer<uint>.Default.Equals(_networkRelatedHealth, value)) return;
				var previous = _networkRelatedHealth;
				_networkRelatedHealth = value;
				if (isClient) SyncBody(previous, value);
			}
		}


	public ICustomTilePassable ICustomTilePassable;

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		SyncBody(RelatedHealth, livingHealth.netId);
		base.OnAddedToBody(livingHealth);
	}

	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
	{
		SyncBody(RelatedHealth, NetId.Empty);
		base.OnRemovedFromBody(livingHealth, source);
	}

	public void SyncBody(uint oldv, uint newv)
	{
		_networkRelatedHealth = newv;
		if (oldv != NetId.Empty)
		{
			var Object = oldv.NetIdToGameObject().GetComponent<MovementSynchronisation>();
			Object.ICustomTilePassable = null;
		}


		if (newv != NetId.Empty)
		{
			var Object = newv.NetIdToGameObject().GetComponent<MovementSynchronisation>();
			Object.ICustomTilePassable = ICustomTilePassable;
		}


	}

	public override void OnInitialize()
	{
		base.OnInitialize();
		ICustomTilePassable = GetComponent<ICustomTilePassable>();
	}

		[System.Serializable]
		private sealed class BehaviorNetworkState
		{
			public uint RelatedHealth;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new BehaviorNetworkState
		{
			RelatedHealth = this.RelatedHealth,
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<BehaviorNetworkState>(json);
			RelatedHealth = state.RelatedHealth;
		}


}
}
