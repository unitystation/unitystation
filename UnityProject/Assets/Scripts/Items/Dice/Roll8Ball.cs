using UnityEngine;

namespace Items.Dice
{
	public class Roll8Ball : RollSpecialDie
	{
		public override string Examine(Vector3 worldPos = default)
		{
			return GetMessage();
		}

		protected override string GetMessage()
		{
			return $"The {dieName} reads; '{specialFaces[result - 1]}'.";
		}
	}
}
