using System.Text;
using NUnit.Framework;
using Util;

namespace Tests
{
	public class EncryptionTest
	{
		[Test]
		public void DecryptingWithCorrectKeyResultsInOriginalMessage()
		{
			var report = new StringBuilder();
			var message = "Testing message. Test. 1234.... , / TeSt";

			var encrypted = EncryptionUtils.Encrypt(message, "test");

			var decrypted = EncryptionUtils.Decrypt(encrypted, "test");

			if(decrypted != message)
			{
				report.AppendLine("Failed encryption/decryption");
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}


		[Test]
		public void DecryptingWithWrongKeyResultsInNothingLikeTheOriginalMessage()
		{
			var report = new StringBuilder();
			var message = "Testing message. Test. 1234.... , / TeSt";

			var encrypted = EncryptionUtils.Encrypt(message, "test");

			var decrypted = EncryptionUtils.Decrypt(encrypted, "bazinga");

			if(decrypted != message)
			{
				report.AppendLine("Failed encryption/decryption");
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}
	}

}
