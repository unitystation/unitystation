using Logs;
using SecureStuff;
using UnityEngine;

namespace UI.Systems.IngameMenu
{
	/// <summary>
	/// Opens a URL. Used in the UI to, for example, link to our Discord server.
	/// </summary>
	public class OpenURL : MonoBehaviour
	{
		[Tooltip("URL to open.")]
		public string url = "https://discordapp.com/invite/fhhQcV9";

		[NaughtyAttributes.Button()]
		public void Open()
		{
			Loggy.LogTrace($"Opening '{url}' in the user's internet browser...");
			SafeURL.Open(url);
		}
	}
}
