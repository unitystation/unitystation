using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecureStuff
{
	/// <summary>
	/// Hides the inspector field When not in play mode
	/// </summary>
	public class PlayModeOnlyAttribute : PropertyAttribute { }

	public class VVNote : Attribute
	{

		public readonly VVHighlight variableHighlightl;

		public VVNote(VVHighlight InvariableHighlightl)
		{
			variableHighlightl = InvariableHighlightl;
		}

	}
	public enum VVHighlight
	{
		None,
		SafeToModify100, //green , = this is great to modify no issues using this
		SafeToModify, //Cyan, It is good to modify but be careful
		UnsafeToModify,//orange, May result in erratic behaviour
		VariableChangeUpdate, //Yellow = Will get immediately overwritten by code so no point modifying
		DEBUG_OrUnecessary, //red = Something that you don't will never modify or is only used for debugging/observing the state
	}
}

