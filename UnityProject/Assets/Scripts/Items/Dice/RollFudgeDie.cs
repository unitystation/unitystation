using UnityEngine;

namespace Items.Dice
{
	public class RollFudgeDie : RollSpecialDie
	{
		public override string Examine(Vector3 worldPos = default)
		{
			return $"It is showing {GetFudgeMessage()}";
		}

		protected override string GetMessage()
		{
			return $"The {dieName} lands {GetFudgeMessage()}";
		}

		private string GetFudgeMessage()
		{
			// Result 2 is a strange side, we modify the formatting such that it reads "a... what?".
			if (result == 2)
			{
				return $"a{specialFaces[1]}";
			}

			return $"a {specialFaces[result - 1]}.";
		}
	}
}
