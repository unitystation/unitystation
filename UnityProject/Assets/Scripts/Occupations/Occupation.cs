
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Defines all aspects of a particular occupation
/// </summary>
[CreateAssetMenu(fileName = "Occupation", menuName = "Occupation/Occupation", order = 1)]
public class Occupation : ScriptableObject
{

	[FormerlySerializedAs("JobType")]
	[SerializeField]
	[Tooltip("Type of occupation.")]
	private JobType jobType;
	public JobType JobType => jobType;

	[Tooltip("Display name for this occupation.")]
	[SerializeField]
	private string displayName;
	public string DisplayName => displayName;

	[FormerlySerializedAs("InventoryPopulator")]
	[SerializeField]
	[Tooltip("Populator to use to populate the player's inventory" +
	         " on spawn when they choose this occupation.")]
	private ItemStoragePopulator inventoryPopulator;
	public ItemStoragePopulator InventoryPopulator => inventoryPopulator;

	[FormerlySerializedAs("Limit")]
	[SerializeField]
	[Tooltip("Maximum simultaneous players with this occupation. Set to -1 for unlimited")]
	private int limit;
	public int Limit => limit;

	[SerializeField]
	[Tooltip("Priority for selecting this occupation when requested occupation is not" +
	         " available.")]
	private int priority;
	public int Priority => priority;

	[FormerlySerializedAs("ChoiceColor")]
	[SerializeField]
	[Tooltip("Color of this occupation's button in the occupation chooser")]
	private Color choiceColor = Color.white;
	public Color ChoiceColor => choiceColor;

	[FormerlySerializedAs("AllowedAccess")]
	[SerializeField]
	[Tooltip("Default access allowed for this occupation.")]
	private List<Access> allowedAccess;
	public List<Access> AllowedAccess => allowedAccess;

}
