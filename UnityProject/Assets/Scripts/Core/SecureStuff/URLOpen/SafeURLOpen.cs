using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Logs;
using UnityEngine;

namespace SecureStuff
{
	public static class SafeURL
	{
		//TODO hub Integration same with http client

		public static async Task Open(string URL, bool AddToAllowList = true, string justificationReason = "")
		{
			URL = URL.ToLower();

			if (URL.StartsWith($"https://"))
			{
				if (TrySanitizeURL(URL, out var goodURL))
				{
					var goodyURL = new System.Uri(goodURL);
					if (await HubValidation.RequestOpenURL(goodyURL, justificationReason, AddToAllowList))
					{
						Loggy.Log($"Opening URL {goodURL}");
						Application.OpenURL(goodURL);
					}
					else
					{
						Loggy.Log("Open URL failed from no user validation");
					}
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
}