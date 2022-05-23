using System.Text;
using System;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace Util
{
	/// <summary>
	/// General tool for encrypting and decrypting strings.
	/// </summary>
	public static class EncryptionUtils
	{
		public static string Encrypt(string clearText, string encryptionKey)
		{
			byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
			byte[] salt = Encoding.ASCII.GetBytes("Cuben Pete");
			using var encryptor = Aes.Create();
			var pdb = new Rfc2898DeriveBytes(encryptionKey,  salt);
			encryptor.Key = pdb.GetBytes(32);
			encryptor.IV = pdb.GetBytes(16);
			using var ms = new MemoryStream();
			using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
			{
				cs.Write(clearBytes, 0, clearBytes.Length);
				cs.Close();
			}
			clearText = Convert.ToBase64String(ms.ToArray());

			return clearText;
		}


		public static string Decrypt(string cipherText, string encryptionKey)
		{
			cipherText = cipherText.Replace(" ", "+");
			byte[] cipherBytes = Convert.FromBase64String(cipherText);
			byte[] salt = Encoding.ASCII.GetBytes("Cuben Pete");
			using var encryptor = Aes.Create();
			var pdb = new Rfc2898DeriveBytes(encryptionKey, salt);
			encryptor.Key = pdb.GetBytes(32);
			encryptor.IV = pdb.GetBytes(16);
			using var ms = new MemoryStream();
			using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
			{
				cs.Write(cipherBytes, 0, cipherBytes.Length);
				cs.Close();
			}
			cipherText = Encoding.Unicode.GetString(ms.ToArray());

			return cipherText;
		}

		/// <summary>
		/// Same as Decrypt but catches the exception in case the key isn't valid and returns an empty string instead.
		/// </summary>
		/// <param name="cipherText"></param>
		/// <param name="encryptionKey"></param>
		/// <returns></returns>
		public static string DecryptSafely(string cipherText, string encryptionKey)
		{
			try
			{
				return Decrypt(cipherText, encryptionKey);
			}
			catch (CryptographicException)
			{
				return string.Empty;
			}
		}

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
				finalLength += random.Next(Math.Clamp(length /2, 0, 100), Math.Clamp(length * 2, 0, 100));
			}
			var randomString = new string(Enumerable.Repeat(chars, finalLength)
				.Select(s => s[random.Next(s.Length)]).ToArray());
			return randomString;
		}
	}
}