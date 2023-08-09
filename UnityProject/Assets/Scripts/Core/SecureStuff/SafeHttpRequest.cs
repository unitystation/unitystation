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
using UnityEngine;

public static class SafeHttpRequest
{
	//TODO
	//Build -> Hub Indication for pop-ups unknown request Hosts

	private static bool TrustedMode = true; //TODO Set in Launch arguments or Via hub

	private static HashSet<string> TrustedHosts = new HashSet<string>(); //Populated as Requests are validated from hub


	public static async Task<HttpResponseMessage> PostAsync(string URLstring, StringContent StringContent)
	{
		var URL = new Uri(URLstring);
		var Client = new HttpClient();
		if (IsValid(URL) == false)
		{
			return null;
		}

		return await Client.PostAsync(URL, StringContent);

	}



	public static async Task<HttpResponseMessage> GetAsync(string URLstring)
	{
		var URL = new Uri(URLstring);
		var Client = new HttpClient();
		if (IsValid(URL) == false)
		{
			return null;
		}

		return await Client.GetAsync(URL);

	}
	public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken? cancellationToken = null)
	{
		var Client = new HttpClient();
		if (IsValid(request.RequestUri) == false)
		{
			return null;
		}

		if (cancellationToken == null)
		{
			return await Client.SendAsync(request);
		}
		else
		{
			return await Client.SendAsync(request, cancellationToken.Value);
		}
	}


	public static async Task<string> GetStringAsync(string requestUri)
	{
		var URL = new System.Uri(requestUri);
		var Client = new HttpClient();
		if (IsValid(URL) == false)
		{
			return null;
		}



		return await Client.GetStringAsync(URL);
	}

	private static bool IsValidatedWithHub(Uri requestUri)
	{
		if (TrustedMode)
		{
			return true;
		}
		if (TrustedHosts.Contains(requestUri.Host))
		{
			return true;
		}


		//TODO A send a message to Hub

		bool IsGood = true;
		if (IsGood)
		{
			TrustedHosts.Add(requestUri.Host);
			return true;
		}
		else
		{
			Logger.LogError($"User declined to access {requestUri}");
			return false;
		}
	}

	public static bool IsValid(Uri requestUri)
	{
		if (requestUri.IsAbsoluteUri == false)
		{
			Logger.LogError($"URL For Request was not absolute e.g Was local such as /blahblahblah/thing {requestUri}");
			return false;
		}

		if (requestUri.IsUnc)
		{
			Logger.LogError($"why IsUnc? Not allowed {requestUri}");
			return false;
		}

		if (Uri.TryCreate(requestUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute, out var baseUri) == false)
		{
			Logger.LogError($"Somehow completely failed URL format check {requestUri}");
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
					Logger.LogError($"HEY BAD Not allowed, Private network IPv4 Address returned for {requestUri}");
					return false;
				}
			}
			else if (AddressToUse.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
			{
				if (IsNotSafeIPv6(AddressToUse))
				{
					Logger.LogError($"HEY BAD Not allowed, Private network IPv6 Address returned for {requestUri}");
					return false;
				}
			}
			else
			{
				Logger.LogError($"Invalid IP Address Return from DNS for {requestUri}");
				return false;
			}
		}

		return true;
	}

	// Check if an IP address falls within the private IP ranges
	public static bool IsNotSafeIPv4(IPAddress ipAddress)
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

	public static bool IsNotSafeIPv6(IPAddress ipAddress)
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

        var  AddressBites =  ipAddress.GetAddressBytes();


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


