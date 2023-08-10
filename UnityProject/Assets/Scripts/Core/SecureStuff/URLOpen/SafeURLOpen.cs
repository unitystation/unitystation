using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

public static class SafeURL
{

	public static void Open(string URL)
	{
		URL = URL.ToLower();

		if (URL.StartsWith($"https://"))
		{
			if (TrySanitizeURL(URL, out var goodURL ))
			{
				Logger.Log($"Opening URL {goodURL}");
				Application.OpenURL(goodURL);
			}
		}
	}

	private static bool TrySanitizeURL(string inputURL, out string sanitizedURL)
	{
		if (Uri.TryCreate(inputURL, UriKind.Absolute, out Uri uriResult) &&
		    uriResult.Scheme == Uri.UriSchemeHttps)
		{
			if (uriResult.IsUnc)
			{
				sanitizedURL = null;
				return false;
			}

			if (IPAddress.TryParse(uriResult.Host, out var IP))
			{
				sanitizedURL = null;
				return false;
			}

			if (uriResult.IsFile)
			{
				sanitizedURL = null;
				return false;
			}

			sanitizedURL = uriResult.AbsoluteUri;
			return true;
		}
		else
		{
			sanitizedURL = null;
			return false;
		}
	}

}
