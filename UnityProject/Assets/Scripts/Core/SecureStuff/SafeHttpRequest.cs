using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logs;
using UnityEngine;

namespace SecureStuff
{

	public static class SafeHttpRequest
	{
		//TODO
		//Build -> Hub Indication for pop-ups unknown request Hosts


		public static async Task<HttpResponseMessage> PostAsync(string URLstring, StringContent StringContent, bool addAsTrusted = true, string JustificationReason = "")
		{
			var URL = new Uri(URLstring);
			var Client = new HttpClient();
			if (await IsValid(URL, addAsTrusted, JustificationReason) == false)
			{
				return null;
			}

			return await Client.PostAsync(URL, StringContent);

		}



		public static async Task<HttpResponseMessage> GetAsync(string URLstring, bool addAsTrusted = true, string JustificationReason = "")
		{
			var URL = new Uri(URLstring);
			var Client = new HttpClient();
			if (await IsValid(URL, addAsTrusted, JustificationReason) == false)
			{
				return null;
			}

			return await Client.GetAsync(URL);

		}

		public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken? cancellationToken = null, bool addAsTrusted = true, string JustificationReason = "")
		{
			using var client = new HttpClient();
			if (await IsValid(request.RequestUri, addAsTrusted, JustificationReason) == false)
			{
				return null;
			}

			if (cancellationToken == null)
			{
				return await client.SendAsync(request);
			}
			else
			{
				return await client.SendAsync(request, cancellationToken.Value);
			}
		}


		public static async Task<string> GetStringAsync(string requestUri, bool addAsTrusted = true, string JustificationReason = "")
		{
			var URL = new System.Uri(requestUri);
			var Client = new HttpClient();
			if (await IsValid(URL, addAsTrusted, JustificationReason) == false)
			{
				return null;
			}


			return await Client.GetStringAsync(URL);
		}

		private static async Task<bool> IsValid(Uri requestUri, bool addAsTrusted = true, string JustificationReason = "")
		{
			if (requestUri.IsAbsoluteUri == false)
			{
				Loggy.LogError(
					$"URL For Request was not absolute e.g Was local such as /blahblahblah/thing {requestUri}");
				return false;
			}

			if (requestUri.IsUnc)
			{
				Loggy.LogError($"why IsUnc? Not allowed {requestUri}");
				return false;
			}

			if (Uri.TryCreate(requestUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute, out var baseUri) == false)
			{
				Loggy.LogError($"Somehow completely failed URL format check {requestUri}");
				// Invalid URI format
				return false;
			}

			IPAddress[] ipAddresses = Dns.GetHostAddresses(baseUri.Host);

			foreach (var AddressToUse in ipAddresses)
			{
				if (AddressToUse.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					if (IsNotSafeIPv4(AddressToUse))
					{
						Loggy.LogError($"HEY BAD Not allowed, Private network IPv4 Address returned for {requestUri}");
						return false;
					}
				}
				else if (AddressToUse.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
				{
					if (IsNotSafeIPv6(AddressToUse))
					{
						Loggy.LogError($"HEY BAD Not allowed, Private network IPv6 Address returned for {requestUri}");
						return false;
					}
				}
				else
				{
					Loggy.LogError($"Invalid IP Address Return from DNS for {requestUri}");
					return false;
				}
			}

			if (await HubValidation.RequestAPIURL(requestUri, JustificationReason, addAsTrusted))
			{
				return true;
			}
			else
			{
				Loggy.Log($"Hub validation failed for {requestUri}");
				return false;
			}

			return true;
		}

		// Check if an IP address falls within the private IP ranges
		private static bool IsNotSafeIPv4(IPAddress ipAddress)
		{
			byte[] addressBytes = ipAddress.GetAddressBytes();

			// Check for private IP ranges:
			// 10.0.0.0 - 10.255.255.255
			// 172.16.0.0 - 172.31.255.255
			// 192.168.0.0 - 192.168.255.255
			return addressBytes[0] == 10 ||
			       (addressBytes[0] == 127) ||
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
			string[] segments = ipAddressString.Split(':');

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
			string[] segments = ipAddressString.Split(':');
			for (int i = 0; i < segments.Length; i++)
			{
				segments[i] = segments[i].TrimStart('0');
				if (string.IsNullOrEmpty(segments[i])) segments[i] = "0";
			}

			return string.Join(":", segments);
		}
	}
}

