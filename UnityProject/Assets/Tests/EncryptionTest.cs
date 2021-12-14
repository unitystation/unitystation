using NUnit.Framework;
using Util;
using System.Security.Cryptography;

namespace Tests
{
	public class EncryptionTest
	{
		private const string RIGHT_KEY = "rightKey";
		private const string WRONG_KEY = "wrongKey";

		private static readonly string[] TestStrings =
		{
			"honk",
			"hello world",
			"123abc",
			"/\\|^$@",
			"\"a string\"",
			"\n\t\r",
			" "
		};

		[TestCaseSource(nameof(TestStrings))]
		public void EncryptedMessageShouldNotBeSameAsOriginal(string testString)
		{
			var encryptedMessage = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			Assert.AreNotEqual(testString, encryptedMessage);
		}

		[TestCaseSource(nameof(TestStrings))]
		public void DecryptingWithRightKeyResultsInOriginalMessage(string testString)
		{
			var encrypted = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			var decrypted = EncryptionUtils.Decrypt(encrypted, RIGHT_KEY);
			Assert.AreEqual(testString, decrypted);
		}

		[TestCaseSource(nameof(TestStrings))]
		public void DecryptingWithWrongKeyResultsInCryptographicException(string testString)
		{
			var encrypted = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			Assert.Throws<CryptographicException>(() => EncryptionUtils.Decrypt(encrypted, WRONG_KEY));
		}

		[TestCaseSource(nameof(TestStrings))]
		public void UsingSameKeyTwiceResultsInSameEncryptedMessage(string testString)
		{
			var encrypted = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			var encryptedAgain = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			Assert.AreEqual(encrypted, encryptedAgain);
		}

		[TestCaseSource(nameof(TestStrings))]
		public void UsingDifferentKeyResultsInDifferentEncryptedMessage(string testString)
		{
			var encrypted = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			var encryptedAgain = EncryptionUtils.Encrypt(testString, WRONG_KEY);
			Assert.AreNotEqual(encrypted, encryptedAgain);
		}

		[TestCaseSource(nameof(TestStrings))]
		public void DecryptingSafelyWithWrongKeyShouldNotThrowCryptographicException(string testString)
		{
			var encrypted = EncryptionUtils.Encrypt(testString, RIGHT_KEY);
			Assert.DoesNotThrow(() => EncryptionUtils.DecryptSafely(encrypted, WRONG_KEY));
		}
	}
}
