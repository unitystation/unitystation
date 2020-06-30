
using UnityEngine;

/// <summary>
/// Defines a specific cooldown with a defined countdown time. Each object in the game
/// can have its own tracked cooldowns via the Cooldowns component.
/// </summary>
[CreateAssetMenu(fileName = "Cooldown", menuName = "Cooldown")]
public class Cooldown : ScriptableObject, ICooldown
{
	[Tooltip("Default time in seconds this cooldown takes to countdown.")]
	[SerializeField]
	private float defaultTime = 0;

	/// <summary>
	/// Default time in seconds this cooldown takes to countdown.
	/// </summary>
	public float DefaultTime => defaultTime;
}
