using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
	public static bool IsUnreasonableNumber(this float Number)
	{
		return float.IsNaN(Number) || float.IsInfinity(Number);
	}
}
