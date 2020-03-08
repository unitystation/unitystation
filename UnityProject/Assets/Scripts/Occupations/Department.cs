
using System.Collections.Generic;
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
	private string displayName;
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
	// Settings defining gameplay-related aspects of the departmen.

	[SerializeField]
	[Tooltip("Occupations associated with this department, and the order in which" +
	         "they should be displayed in job preferences.")]
	private Occupation[] occupations;
	public IEnumerable<Occupation> Occupations => occupations;
}
