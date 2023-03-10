using System;
using System.Collections.Generic;
using System.Net;

namespace Core.Database
{
	/// <summary>
	/// Error class for any usage-specific API errors as returned by the API server.
	/// </summary>
	public class ApiRequestException : Exception
	{
		/// <summary>A list of all error messages returned by the API server.</summary>
		/// <remarks>You can use <c>Message</c> to get the first one.</remarks>
		public List<string> Messages { get; set; }

		public ApiRequestException(string message) : base(message)
		{
			Messages = new List<string>();
		}
	}

	/// <summary>
	/// Error class for any HTTP-related errors as returned by the API server.
	/// </summary>
	public class ApiHttpException : Exception
	{
		public HttpStatusCode StatusCode { get; private set; }

		public ApiHttpException(string message, HttpStatusCode code) : base(message)
		{
			StatusCode = code;
		}
	}
}
