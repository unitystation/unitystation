using System.Net.Http;

namespace Core.Networking
{
	/// <summary>
	/// <see cref="HttpClient"/> provider.
	/// As per .NET recommendation, we should keep and reuse one HttpClient object for the whole application.
	/// </summary>
	// TODO: consider factory, threading considerations
	public static class Http
	{
		public static HttpClient Client = new();
	}
}
 