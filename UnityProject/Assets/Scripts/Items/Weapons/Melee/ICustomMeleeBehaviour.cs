using UnityEngine;

/// <summary>
/// Invoked before the majority of WeaponNetworkActions, allowing for custom melee behaviours like backstabbing.
/// If all you want is to tack on an extra effect like a stun, use ItemAttributesV2's OnMelee action
/// </summary>
public interface ICustomMeleeBehaviour
{
	WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats);
}