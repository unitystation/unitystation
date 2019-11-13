
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines an ordering for right click options so they appear in a consistent
/// order in the radial menu.
/// </summary>
[CreateAssetMenu(fileName = "new RightClickOptionOrder", menuName = "Interaction/Right Click Option Order")]
public class RightClickOptionOrder : ScriptableObject
{
	[Tooltip("Order in which each right click option should appear. Any" +
	         " options omitted from this list will be placed at the end in alphabetical order of their label.")]
	public List<RightClickOption> orderedOptions;


	/// <summary>
	/// Return an int (same logic as IComparable) ordering the right click options according to the list
	/// defined in this RightClickOptionOrder
	/// </summary>
	/// <param name="c1"></param>
	/// <param name="c2"></param>
	/// <returns>-1 if c2 should go first, 1 if c1 should go first, 0 if they are the same</returns>
	public int Compare(RightClickOption c1, RightClickOption c2)
	{
		var p1 = orderedOptions.IndexOf(c1);
		var p2 = orderedOptions.IndexOf(c2);
		if (p1 == -1 && p2 == -1)
		{
			return String.Compare(c1.label, c2.label, StringComparison.Ordinal);
		}

		if (p1 == -1)
		{
			return 1;
		}

		if (p2 == -1)
		{
			return -1;
		}

		return p1.CompareTo(p2);
	}
}
