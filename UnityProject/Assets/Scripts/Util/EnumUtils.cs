namespace Util
{
	public static class EnumUtils
	{
		public static int GetSetBitCount(long lValue)
		{
			int iCount = 0;

			while (lValue != 0)
			{
				lValue = lValue & (lValue - 1);
				iCount++;
			}

			return iCount;
		}
	}
}