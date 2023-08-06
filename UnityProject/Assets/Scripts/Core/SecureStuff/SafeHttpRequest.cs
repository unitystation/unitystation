using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class SafeHttpRequest
{
	public static async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken? cancellationToken = null)
	{
		var Client = new HttpClient();
		if (IsValid(request.RequestUri) == false)
		{
			Logger.LogError($"Provided URL for API request was blocked due to pointing to an IP URL {request.RequestUri}");
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
			Logger.LogError($"Provided URL for API request was blocked due to pointing to an IP URL {requestUri}");
			return null;
		}

		return await Client.GetStringAsync(URL);
	}


	public static bool IsValid(Uri requestUri)
	{
		if (requestUri.IsAbsoluteUri == false)
		{
			return false;
		}

		if (requestUri.IsUnc)
		{
			return false;
		}

		if (Uri.TryCreate(requestUri.GetLeftPart(UriPartial.Authority), UriKind.Absolute, out var baseUri) == false)
		{
			// Invalid URI format
			return false;
		}


		switch (baseUri.HostNameType)
		{
			case UriHostNameType.Dns:
				return true;
			case UriHostNameType.IPv4:
			case UriHostNameType.IPv6:
			default:
				return false;
		}
	}
}