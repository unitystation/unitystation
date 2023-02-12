namespace Util
{
	internal static class StringExt
	{
		/// <summary>
		/// Capitalizes the first letter in a string. Does not check for multiple words, only the first character in the string array.
		/// </summary>
		/// <param name="str">The string you want to manipulate.</param>
		/// <returns>The same string with the first character in upper case.</returns>
		public static string CapitalizeFirstLetter(this string str)
		{
			if (str.Length == 0) return str;
			var result = str.ToCharArray();
			result[0] = char.ToUpper(result[0]);
			return new string(result);
		}
	}
}