using System.Text;
using System;
using System.IO;

using System.Linq;

namespace Util
{
	/// <summary>
	/// General tool for encrypting and decrypting strings.
	/// </summary>
	public static class EncryptionUtils
	{
		/// <summary>
		/// A fast but ugly encryption/decryption function.
		/// </summary>
		/// <param name="inputData">string to decrypt/encrypt</param>
		/// <param name="key">EncryptionSecret</param>
		/// <returns></returns>
		public static string XOREncryptDecrypt(string inputData, int key)
		{
			var outSB = new StringBuilder(inputData.Length);
			foreach (var character in inputData)
			{
				//Here 1234 is key for Encrypt/Decrypt, You can use any int number
				char ch = (char)(character ^ key);
				outSB.Append(ch);
			}

			if (Encoding.GetEncoding(outSB.ToString()) == Encoding.UTF8)
			{
				byte[] array = Encoding.ASCII.GetBytes(outSB.ToString());
				return Encoding.Convert(Encoding.UTF8, Encoding.Unicode, array).ToString();
			}
			return outSB.ToString();
		}

		/// <summary>
		/// Generates a random alphanumeric string.
		/// </summary>
		/// <param name="length">The desired length of the string</param>
		/// <returns>The string which has been generated</returns>
		public static string GenerateRandomAlphanumericString(int length, bool randomizeLength = true)
		{
			var finalLength = length;
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

			var random = new Random();

			if (randomizeLength)
			{
				finalLength = random.Next(Math.Clamp(length /2, 0, 100), Math.Clamp(length * 2, 0, 100));
			}
			var randomString = new string(Enumerable.Repeat(chars, finalLength)
				.Select(s => s[random.Next(s.Length)]).ToArray());
			return randomString;
		}
	}
}