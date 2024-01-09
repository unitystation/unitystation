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
		SafeToModify100, //green
		SafeToModify, //Cyan
		UnsafeToModify,//orange
		VariableChangeUpdate, //Yellow
		DEBUG, //red
	}
}

