using UnityEngine;

public static class YieldHelper
{
	public static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
	public static readonly WaitForSeconds DeciSecond = new WaitForSeconds(0.1f);
	public static readonly WaitForSeconds Second = new WaitForSeconds(1f);
}