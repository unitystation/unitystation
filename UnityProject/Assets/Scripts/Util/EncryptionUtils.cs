using System.Text;
using System;
using System.Security.Cryptography;
using System.IO;

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
			using (Aes encryptor = Aes.Create())
			{
				Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey,  salt);
				encryptor.Key = pdb.GetBytes(32);
				encryptor.IV = pdb.GetBytes(16);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
					{
						cs.Write(clearBytes, 0, clearBytes.Length);
						cs.Close();
					}
					clearText = Convert.ToBase64String(ms.ToArray());
				}
			}
			return clearText;
		}


		public static string Decrypt(string cipherText, string encryptionKey)
		{
			cipherText = cipherText.Replace(" ", "+");
			byte[] cipherBytes = Convert.FromBase64String(cipherText);
			byte[] salt = Encoding.ASCII.GetBytes("Cuben Pete");
			using (Aes encryptor = Aes.Create())
			{
				Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, salt);
				encryptor.Key = pdb.GetBytes(32);
				encryptor.IV = pdb.GetBytes(16);
				using (MemoryStream ms = new MemoryStream())
				{
					using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
					{
						cs.Write(cipherBytes, 0, cipherBytes.Length);
						cs.Close();
					}
					cipherText = Encoding.Unicode.GetString(ms.ToArray());
				}
			}
			return cipherText;
		}

		public static string XOREncryptDecrypt(string inputData, int key)
		{
			StringBuilder outSB = new StringBuilder(inputData.Length);
			for (int i = 0; i < inputData.Length; i++)
			{
				//Here 1234 is key for Encrypt/Decrypt, You can use any int number
				char ch = (char)(inputData[i] ^ key);
				outSB.Append(ch);
			}
			return outSB.ToString();
		}
	}
}