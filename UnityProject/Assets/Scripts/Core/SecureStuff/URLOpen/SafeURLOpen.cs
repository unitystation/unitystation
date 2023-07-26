using System;
using System.Collections;
using System.Collections.Generic;
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
			// Optionally, you can add additional checks here based on your requirements.
			// For example, you may want to check for specific domains in a whitelist.
			if (uriResult.IsUnc)
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
