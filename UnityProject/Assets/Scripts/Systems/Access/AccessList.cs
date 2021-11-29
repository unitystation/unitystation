using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to sort accesses by type
/// </summary>
[CreateAssetMenu(fileName = "AccessList", menuName = "ScriptableObjects/AccessList")]
public class AccessList : ScriptableObject
{
	[SerializeField]
	private List<Access> accesses = null;
	public List<Access> Accesses => accesses;
}
