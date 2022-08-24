using System.Text.RegularExpressions;

namespace Core.Utils
{
	public static class ValidationUtils
	{
		public enum StringValidateError
		{
			None = 0,
			NullOrWhitespace,
			TooShort,
			Invalid,
		}

		/// <summary>
		/// Determine whether the given email string is considered a valid email according to RFC 5322.
		/// </summary>
		/// <param name="email">email to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidateEmail(string email, out StringValidateError failReason)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				failReason = StringValidateError.NullOrWhitespace;
				return false;
			}

			// 👀 courtesy of https://uibakery.io/regex-library/email-regex-csharp
			string pattern = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";

			if (new Regex(pattern).IsMatch(email) == false)
			{
				failReason = StringValidateError.None;
				return false;
			}

			failReason = default;
			return true;
		}

		/// <summary>
		/// Determine whether the given Unitystation account username string is considered a valid username.
		/// </summary>
		/// <param name="username">the username to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidateUsername(string username, out StringValidateError failReason)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				failReason = StringValidateError.NullOrWhitespace;
				return false;
			}

			// TODO: consider determining actual minimum length.
			if (username.Length < 3)
			{
				failReason = StringValidateError.TooShort;
				return false;
			}

			// TODO: consider a regex test to ensure legal characters only.

			failReason = StringValidateError.None;
			return true;
		}

		/// <summary>
		/// Determine whether the given Unitystation account password string is considered a valid password.
		/// </summary>
		/// <param name="password">the password to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidatePassword(string password, out StringValidateError failReason)
		{
			if (string.IsNullOrWhiteSpace(password))
			{
				failReason = StringValidateError.NullOrWhitespace;
				return false;
			}

			// TODO: consider determining actual minimum length.
			if (password.Length < 3)
			{
				failReason = StringValidateError.TooShort;
				return false;
			}

			// TODO: consider a regex test to ensure legal characters only.

			failReason = StringValidateError.None;
			return true;
		}
	}
}
