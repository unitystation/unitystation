
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines all aspects of a particular occupation
/// </summary>
[CreateAssetMenu(fileName = "Occupation", menuName = "Occupation/Occupation", order = 1)]
public class Occupation : ScriptableObject
{
	[Tooltip("Type of occupation.")]
	public JobType JobType;

	[Tooltip("Populator to use to populate the player's inventory" +
	         " on spawn when they choose this occupation.")]
	public ItemStoragePopulator InventoryPopulator;

	[Tooltip("Maximum simultaneous players with this occupation. Set to -1 for unlimited")]
	public int Limit;

	[Tooltip("Priority for selecting this occupation when requested occupation is not" +
	         " available.")]
	public int priority;

	[Tooltip("Color of this occupation's button in the occupation chooser")]
	public Color ChoiceColor = Color.white;

	[Tooltip("Default access allowed for this occupation.")]
	public List<Access> AllowedAccess;
}
