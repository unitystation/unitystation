using System.Collections;
using System.Collections.Generic;
using Systems.Clearance;
using UnityEngine;

/// <summary>
/// Used to sort accesses by type
/// </summary>
[CreateAssetMenu(fileName = "AccessList", menuName = "ScriptableObjects/AccessList")]
public class AccessList : ScriptableObject
{
	[SerializeField]
	private List<Clearance> clearances = null;
	public List<Clearance> Clearances => clearances;
}
