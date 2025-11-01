using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Logs;

namespace SecureStuff
{
	public static class SafeHttpRequest
	{
		//TODO
		//Build -> Hub Indication for pop-ups unknown request Hosts
		private static HttpClient Client = new();

#if UNITY_EDITOR
		public static HttpClient EditorOnlySet
		{
			set => Client = value;
		}
#endif

		public static async Task<HttpResponseMessage> PostAsync(string urlString, StringContent stringContent,
			bool addAsTrusted = true, string justificationReason = "")
		{
			var url = new Uri(urlString);
			if (await IsValid(url, addAsTrusted, justificationReason) == false)
			{
				return null;
			}

			return await Client.PostAsync(url, stringContent);
		}


		public static async Task<HttpResponseMessage> GetAsync(string urlString, bool addAsTrusted = true,
			string justificationReason = "")
		{
			var url = new Uri(urlString);
			if (await IsValid(url, addAsTrusted, justificationReason) == false)
			{
				return null;
			}

			return await Client.GetAsync(url);
		}

		public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken? cancellationToken = null, bool addAsTrusted = true, string justificationReason = "")
		{
			if (await IsValid(request.RequestUri, addAsTrusted, justificationReason) == false)
			{
				return null;
			}

			if (cancellationToken == null)
			{
				return await Client.SendAsync(request);
			}

			return await Client.SendAsync(request, cancellationToken.Value);
		}


		public static async Task<string> GetStringAsync(string requestUri, bool addAsTrusted = true,
			string justificationReason = "")
		{
			var url = new Uri(requestUri);
			if (await IsValid(url, addAsTrusted, justificationReason) == false)
			{
				return null;
			}

			return await Client.GetStringAsync(url);
		}

		private static async Task<bool> IsValid(Uri requestUri, bool addAsTrusted = true,
			string justificationReason = "")
		{
			if (requestUri == null)
			{
				Loggy.Error("Rejecting HTTP request: request URI is null.");
				return false;
			}

			if (requestUri.IsAbsoluteUri == false)
			{
				Loggy.Error($"Rejecting HTTP request to '{requestUri}': URL must be absolute.");
				return false;
			}

			if (requestUri.IsUnc)
			{
				Loggy.Error($"Rejecting HTTP request to '{requestUri}': UNC paths are not allowed.");
				return false;
			}

			if (Uri.TryCreate(requestUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute, out var baseUri) == false)
			{
				Loggy.Error($"Rejecting HTTP request to '{requestUri}': unable to parse authority segment.");
				return false;
			}

			if (HostResolvesToPublicAddress(requestUri, baseUri) == false)
			{
				return false;
			}

			if (await HubValidation.RequestAPIURL(requestUri, justificationReason, addAsTrusted))
			{
				return true;
			}

			var justificationInfo = string.IsNullOrWhiteSpace(justificationReason) ? "none provided" : justificationReason;
			Loggy.Info(
				$"Hub validation rejected request to '{requestUri}'. Justification: {justificationInfo}.");
			return false;
		}

		private static bool HostResolvesToPublicAddress(Uri requestUri, Uri baseUri)
		{
			IPAddress[] ipAddresses;
			try
			{
				ipAddresses = Dns.GetHostAddresses(baseUri.Host);
			}
			catch (SocketException ex)
			{
				Loggy.Error(
					$"Rejecting HTTP request to '{requestUri}': DNS lookup failed for host '{baseUri.Host}'. {ex.Message}");
				return false;
			}
			catch (Exception ex)
			{
				Loggy.Error(
					$"Rejecting HTTP request to '{requestUri}': unexpected error resolving host '{baseUri.Host}'. {ex.Message}");
				return false;
			}

			if (ipAddresses.Length == 0)
			{
				Loggy.Error(
					$"Rejecting HTTP request to '{requestUri}': DNS lookup returned no addresses for host '{baseUri.Host}'.");
				return false;
			}

			foreach (var address in ipAddresses)
			{
				switch (address.AddressFamily)
				{
					case AddressFamily.InterNetwork:
						if (IsNotSafeIPv4(address))
						{
							Loggy.Error(
								$"Rejecting HTTP request to '{requestUri}': IPv4 address '{address}' resolves inside a private network.");
							return false;
						}

						break;
					case AddressFamily.InterNetworkV6:
						if (IsNotSafeIPv6(address))
						{
							Loggy.Error(
								$"Rejecting HTTP request to '{requestUri}': IPv6 address '{address}' resolves inside a private network.");
							return false;
						}

						break;
					default:
						Loggy.Error(
							$"Rejecting HTTP request to '{requestUri}': unsupported address family '{address.AddressFamily}' for resolved address '{address}'.");
						return false;
				}
			}

			return true;
		}

		// Check if an IP address falls within the private IP ranges
		private static bool IsNotSafeIPv4(IPAddress ipAddress)
		{
			var addressBytes = ipAddress.GetAddressBytes();

			// Check for private IP ranges:
			// 10.0.0.0 - 10.255.255.255
			// 172.16.0.0 - 172.31.255.255
			// 192.168.0.0 - 192.168.255.255
			return addressBytes[0] == 10 ||
			       addressBytes[0] == 127 ||
			       (addressBytes[0] == 172 && addressBytes[1] >= 16 && addressBytes[1] <= 31) ||
			       (addressBytes[0] == 192 && addressBytes[1] == 168);
		}

		private static bool IsNotSafeIPv6(IPAddress ipAddress)
		{
			var ipAddressString = ipAddress.ToString();
			// Remove any leading zeros from the input IPv6 address
			ipAddressString = RemoveLeadingZeros(ipAddressString);

			if (Equals(ipAddress, IPAddress.Parse("::1")))
			{
				return true;
			}

			//::ffff:0.0.0.0   ::ffff:255.255.255.255
			//::ffff:0:0.0.0.0 ::ffff:0:255.255.255.255
			if (ipAddressString.Contains("."))
			{
				return true;
			}

			//64:ff9b:1:: - 64:ff9b:1:ffff:ffff:ffff:ffff:ffff
			var segments = ipAddressString.Split(':');

			if (segments[0] == "64")
			{
				if (segments[1] == "ff9b")
				{
					if (segments[2].StartsWith("1"))
					{
						return true;
					}
				}
			}

			//fc00:: - fdff:ffff:ffff:ffff:ffff:ffff:ffff:ffff
			// IPAddress startRange2 = IPAddress.Parse("fc00::");
			// IPAddress endRange2 = IPAddress.Parse("fdff::");
			byte Start = 252;
			byte End = 253;

			var AddressBites = ipAddress.GetAddressBytes();


			if (AddressBites[0] >= Start)
			{
				if (AddressBites[0] <= End)
				{
					return true;
				}
			}

			return false;
		}

		private static string RemoveLeadingZeros(string ipAddressString)
		{
			// Split the address by ':' to remove leading zeros from each segment
			var segments = ipAddressString.Split(':');
			for (var i = 0; i < segments.Length; i++)
			{
				segments[i] = segments[i].TrimStart('0');
				if (string.IsNullOrEmpty(segments[i]))
				{
					segments[i] = "0";
				}
			}

			return string.Join(":", segments);
		}
	}
}
