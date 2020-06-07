
using UnityEngine;

/// <summary>
/// Commonly used cooldowns for easier referencing in code.
/// </summary>
[CreateAssetMenu(fileName = "CommonCooldownSingleton", menuName = "Singleton/CommonCooldowns")]
public class CommonCooldowns : SingletonScriptableObject<CommonCooldowns>
{
	/// <summary>
	/// Base cooldown used for all interaction logic (generally quite a low value, the maximum speed we would
	/// ever want someone to be able to perform any kind of action).
	/// </summary>
	public Cooldown Interaction;
	/// <summary>
	/// Cooldown for melee-type interactions. Generally somewhat high so that combat actions
	/// cannot be quickly spammed.
	/// </summary>
	public Cooldown Melee;
	/// <summary>
	/// Cooldown for vending new items from vending machines.
	/// </summary>
	public Cooldown Vending;
}
