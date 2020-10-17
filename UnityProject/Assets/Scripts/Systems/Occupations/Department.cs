
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Defines all aspects of a particular department
/// </summary>
[CreateAssetMenu(fileName = "Department", menuName = "Occupation/Department", order = 1)]
public class Department : ScriptableObject
{
	[Header("Description")]
	// Information that has no real gameplay impact, but is very useful for the player to see.

	[SerializeField]
	[Tooltip("Display name for this department.")]
	private string displayName = null;
	public string DisplayName => displayName;

	[SerializeField]
	[Tooltip("A description of this department's duties.")]
	private string description = "";
	// TODO: write these and use them somewhere
	public string Description => description;

	[SerializeField]
	[Tooltip("Color of this department's header in the job preferences screen")]
	private Color headerColor = Color.gray;
	public Color HeaderColor => headerColor;

	[SerializeField]
	[Tooltip("Icon for the department.")]
	private Sprite icon = null;
	// TODO: make these and use them somewhere
	public Sprite Icon => icon;

	[Header("Settings")]
	// Settings defining gameplay-related aspects of the department.

	[SerializeField]
	[Tooltip("Occupations which are the heads of this department, can include multiple or be empty. " +
			 "Used for job allocation and will appear at the top of the department in job preferences.")]
	private List<Occupation> headOccupations = new List<Occupation>();
	/// <summary>
	/// All head of department occupations
	/// </summary>
	public IEnumerable<Occupation> HeadOccupations => headOccupations;

	[SerializeField]
	[Tooltip("Occupations associated with this department, and the order in which" +
			 "they should be displayed in job preferences.")]
	private List<Occupation> occupations = new List<Occupation>();
	/// <summary>
	/// All non-head of department occupations for this department
	/// </summary>
	public IEnumerable<Occupation> Occupations => occupations;

	/// <summary>
	/// All head and non-head occupations in this department
	/// </summary>
	public IEnumerable<Occupation> AllOccupations => headOccupations.Concat(occupations);
}
