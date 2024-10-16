using UnityEngine;

/// <summary>
/// Randomizes damage whenever a swing occurs.
/// </summary>
public class ExtradimBlade : MonoBehaviour, ICustomMeleeBehaviour
{
	[SerializeField]
	private int minDamage = 1;

	[SerializeField]
	private int maxDamage = 30;

	private static System.Random rnd = new System.Random();

	public WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
	{
		var modStats = stats;
		modStats.Damage = rnd.Next(minDamage, maxDamage);
		return modStats;
	}
}