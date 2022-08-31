using System;
using System.Text.RegularExpressions;

namespace Core.Utils
{
	/// <summary>
	/// A collection of utility methods for value validation.
	/// </summary>
	public static class ValidationUtils
	{
		public enum ValidationError
		{
			None = 0,
			NullOrWhitespace,
			TooShort,
			Invalid,
		}

		/// <summary>
		/// Determine whether the given server address is considered a valid host name or address.
		/// </summary>
		/// <param name="port">address to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidateAddress(string port, out ValidationError failReason)
		{
			if (string.IsNullOrWhiteSpace(port))
			{
				failReason = ValidationError.NullOrWhitespace;
				return false;
			}

			UriHostNameType addressType = Uri.CheckHostName(port);
			if (addressType == UriHostNameType.Unknown || addressType == UriHostNameType.Basic)
			{
				failReason = ValidationError.Invalid;
				return false;
			}

			failReason = default;
			return true;
		}

		/// <summary>
		/// Determine whether the given server port is considered a valid port.
		/// </summary>
		/// <param name="port">port to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidatePort(string port, out ValidationError failReason)
		{
			if (string.IsNullOrWhiteSpace(port))
			{
				failReason = ValidationError.NullOrWhitespace;
				return false;
			}

			// If it can be represented as an unsigned 16-bit integer, it's valid.
			if (ushort.TryParse(port, out _) == false) {
				failReason = ValidationError.Invalid;
				return false;
			}

			failReason = default;
			return true;
		}

		/// <summary>
		/// Determine whether the given email string is considered a valid email according to RFC 5322.
		/// </summary>
		/// <param name="email">email to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidateEmail(string email, out ValidationError failReason)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				failReason = ValidationError.NullOrWhitespace;
				return false;
			}

			// 👀 courtesy of https://uibakery.io/regex-library/email-regex-csharp
			string pattern = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";

			if (new Regex(pattern).IsMatch(email) == false)
			{
				failReason = ValidationError.Invalid;
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
		public static bool TryValidateUsername(string username, out ValidationError failReason)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				failReason = ValidationError.NullOrWhitespace;
				return false;
			}

			// TODO: consider determining actual minimum length.
			if (username.Length < 3)
			{
				failReason = ValidationError.TooShort;
				return false;
			}

			// TODO: consider a regex test to ensure legal characters only.

			failReason = ValidationError.None;
			return true;
		}

		/// <summary>
		/// Determine whether the given Unitystation account password string is considered a valid password.
		/// </summary>
		/// <param name="password">the password to validate</param>
		/// <returns>true if valid</returns>
		public static bool TryValidatePassword(string password, out ValidationError failReason)
		{
			if (string.IsNullOrWhiteSpace(password))
			{
				failReason = ValidationError.NullOrWhitespace;
				return false;
			}

			// TODO: consider determining actual minimum length.
			if (password.Length < 3)
			{
				failReason = ValidationError.TooShort;
				return false;
			}

			// TODO: consider a regex test to ensure legal characters only.

			failReason = ValidationError.None;
			return true;
		}
	}
}
