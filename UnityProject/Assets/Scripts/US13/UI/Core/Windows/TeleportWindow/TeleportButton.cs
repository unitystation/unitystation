using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Core.Windows.TeleportWindow
{
	/// <summary>
	/// Script attached to the teleport buttons for TeleportWindow.
	/// </summary>
	public class TeleportButton : MonoBehaviour
	{
		[SerializeField]
		public Text myText;

		public TeleportInfo TeleportInfo { get; private set; }

		private TeleportWindow teleportWindow;

		/*public int index;

		public bool MobTeleport = false;

		private GhostTeleport ghostTeleport;*/

		public void SetValues(TeleportWindow teleportWindow, TeleportInfo teleportInfo)
		{
			this.teleportWindow = teleportWindow;
			TeleportInfo = teleportInfo;

			SetTeleportButtonText($"{TeleportInfo.text}\n{TeleportInfo.position}");
		}

		private void SetTeleportButtonText(string textString)
		{
			myText.text = textString;
		}

		public void Onclick()
		{
			teleportWindow.ButtonClicked(TeleportInfo);

			/*ghostTeleport = GetComponentInParent<GhostTeleport>();

			ghostTeleport.Button

			if (MobTeleport == true)
			{
				ghostTeleport.DataForTeleport(index);// Gives index to GhostTeleport.cs
			}
			else
			{
				ghostTeleport.PlacesDataForTeleport(index);// Gives index to GhostTeleport.cs
			}*/
		}
	}
}
