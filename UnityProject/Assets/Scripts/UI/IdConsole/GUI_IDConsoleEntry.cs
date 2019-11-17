
using System;
using UnityEngine;

/// <summary>
/// New ID console entry. Manages the logic of an individual button on the ID
/// console, which may be an assignment (like HOS, Miner, etc...) or an individual access (mining office, rnd lab, etc...)
/// </summary>
public class GUI_IDConsoleEntry : MonoBehaviour
{
	//This button is used in two types - as access and assignment
	[Tooltip("Whether this is an assignment (occupation) or an access (individual permission)")]
	[SerializeField]
	private bool isAssignment;
	[Tooltip("If assignment, occupation this button will grant.")]
	[SerializeField]
	private Occupation occupation;
	[Tooltip("If access, access this button will grant")]
	private Access access;

	//parent ID console tab this lives in
	private GUI_IdConsole console;

	private void Awake()
	{
		throw new NotImplementedException();
	}
}
