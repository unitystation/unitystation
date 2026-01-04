using System;
using HealthV2;
using Mirror;
using UnityEngine;

public class BodyPartCustomMovement : BodyPartFunctionality
{

	[SyncVar(hook = nameof(SyncBody))] public uint RelatedHealth;


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
		RelatedHealth = newv;
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

	public void Awake()
	{
		base.Awake();
		ICustomTilePassable = GetComponent<ICustomTilePassable>();
	}


}
