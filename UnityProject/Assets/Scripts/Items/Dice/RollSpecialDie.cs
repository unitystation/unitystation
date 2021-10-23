using System.Collections.Generic;
using UnityEngine;

namespace Items.Dice
{
	/// <summary>
	/// For die that have special faces (shows the name of the face instead of the number).
	/// </summary>
	public class RollSpecialDie : RollDie
	{
		[SerializeField]
		[Tooltip("A list of the possible side names.")]
		protected List<string> specialFaces = new List<string>();

		public override string Examine(Vector3 worldPos = default)
		{
			return $"It is showing side '{specialFaces[result - 1]}'.";
		}

		protected override string GetMessage()
		{
			return $"The {dieName} lands a '{specialFaces[result - 1]}'.";
		}
	}
}
