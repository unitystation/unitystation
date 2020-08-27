using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Opens a URL. Used in the UI to, for example, link to our Discord server.
/// </summary>
public class OpenURL : MonoBehaviour
{
	[Tooltip("URL to open.")]
	public string url = "https://discordapp.com/invite/fhhQcV9";

	public void Open()
	{
		print($"Opening '{url}' in the user's internet browser...");
		Application.OpenURL(url);
	}
}
