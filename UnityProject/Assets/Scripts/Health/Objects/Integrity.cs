
using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Objects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Object = System.Object;

/// <summary>
/// Component which allows an object to have an integrity value (basically an object's version of HP),
/// take damage, and do things in response to integrity changes. Objects are destroyed when their integrity
/// reaches 0.
///
/// This stuff is tracked server side only, client is informed only when the effects of integrity
/// changes occur.
/// </summary>
[RequireComponent(typeof(CustomNetTransform))]
[RequireComponent(typeof(Meleeable))]
public class Integrity : NetworkBehaviour, IRightClickable
{

	/// <summary>
	/// Server-side event invoked when object integrity reaches 0 by any means and object
	/// destruction logic is about to be invoked. Does not override the default destruction logic,
	/// simply provides a hook for when it is going to be invoked.
	/// </summary>
	[NonSerialized] public DestructionEvent OnWillDestroyServer = new DestructionEvent();

	/// <summary>
	/// Armor for this object. This can be set in inspector but some
	/// components might override this value in code.
	/// </summary>
	[Tooltip("Armor for this object. This can be set in inspector but some" +
	         " components might override this value in code.")]
	public Armor Armor = new Armor();

	private float integrity = 100f;
	private bool destroyed;
	private DamageType lastDamageType;

	//unitystation spawn hook
	private void OnSpawnedServer()
	{
		integrity = 100f;
		destroyed = false;
	}

	/// <summary>
	/// Directly deal damage to this object.
	/// </summary>
	/// <param name="damage"></param>
	/// <param name="damageType"></param>
	[Server]
	public void ApplyDamage(float damage, AttackType attackType, DamageType damageType)
	{
		//already destroyed, don't apply damage
		if (destroyed) return;

		damage = Armor.GetDamage(damage, attackType);
		if (damage > 0)
		{
			integrity -= damage;
			lastDamageType = damageType;
			CheckDestruction();
			Logger.LogTraceFormat("{0} took {1} {2} damage from {3} attack (resistance {4}) (integrity now {5})", Category.Health, name, damage, damageType, attackType, Armor.GetRating(attackType), integrity);
		}
	}

	[Server]
	private void CheckDestruction()
	{
		if (!destroyed && integrity <= 0)
		{
			var destructInfo = new DestructionInfo(lastDamageType);
			OnWillDestroyServer.Invoke(destructInfo);

			DefaultDestroy(destructInfo);
			destroyed = true;
		}
	}

	[Server]
	private void DefaultDestroy(DestructionInfo info)
	{
		if (info.DamageType == DamageType.Brute)
		{
			ChatRelay.Instance.AddToChatLogServer(ChatEvent.Local($"{name} was smashed to pieces.", gameObject.TileWorldPosition()));
			PoolManager.PoolNetworkDestroy(gameObject);
		}
		//TODO: Other damage types (acid)
		else
		{
			ChatRelay.Instance.AddToChatLogServer(ChatEvent.Local($"{name} was destroyed.",gameObject.TileWorldPosition()));
			PoolManager.PoolNetworkDestroy(gameObject);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		return RightClickableResult.Create()
			.AddAdminElement("Smash", AdminSmash);
	}

	private void AdminSmash()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdAdminSmash(gameObject);
	}
}

/// <summary>
/// Contains info on how an object was destroyed
/// </summary>
public class DestructionInfo
{
	/// <summary>
	/// Damage that destroyed the object
	/// </summary>
	public readonly DamageType DamageType;

	public DestructionInfo(DamageType damageType)
	{
		DamageType = damageType;
	}
}

/// <summary>
/// Event fired when an object is destroyed
/// </summary>
public class DestructionEvent : UnityEvent<DestructionInfo>{}
