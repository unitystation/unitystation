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

		public void Open()
		{
			Logger.LogTrace($"Opening '{url}' in the user's internet browser...");
			Application.OpenURL(url);
		}
	}
}
