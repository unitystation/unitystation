using UnityEngine;

public class RollFudgeDie : RollSpecialDie
{
	public override string Examine(Vector3 worldPos = default)
	{
		return $"It is showing side {GetFudgeMessage()}";
	}

	protected override string GetMessage()
	{
		return $"The {dieName} lands a {GetFudgeMessage()}";
	}

	private string GetFudgeMessage()
	{
		if (result == 2)
		{
			return specialFaces[1].ToString();
		}
		
		return $"{specialFaces[result - 1]}.";
	}
}
