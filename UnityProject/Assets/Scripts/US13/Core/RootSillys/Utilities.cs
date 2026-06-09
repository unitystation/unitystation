namespace Core.RootSillys
{
	public static class Utilities
	{
		public static bool IsUnreasonableNumber(this float Number)
		{
			return float.IsNaN(Number) || float.IsInfinity(Number);
		}

		public static float MakeInToReasonableNumber(this float Number, float Reasonable)
		{
			if (Number.IsUnreasonableNumber())
			{
				return Reasonable;
			}
			else
			{
				return Number;
			}
		}
	}
}
