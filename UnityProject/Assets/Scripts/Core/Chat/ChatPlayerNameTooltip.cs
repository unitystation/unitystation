using Logs;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class ChatPlayerNameTooltip : TooltipMonoBehaviour
{
	// 26 is length of <color=#00000000></color>, +1 to make sure that name is not empty
	private const int COLOR_TAG_LENGTH = 27;

	[SerializeField] private TMP_Text playeNameText = default;

	public override string Tooltip
	{
		get
		{
			if (playeNameText == null)
			{
				Loggy.LogWarning("playeNameText is null", Category.Chat);
				return string.Empty;
			}

			if(playeNameText.text.Length < COLOR_TAG_LENGTH)
				return string.Empty;

			int colorIndex = playeNameText.text.IndexOf('=') + 2; // + 2 to skip '=' and '#'
			string colorString = playeNameText.text.Substring(colorIndex, 8); // get 8 chars RRGGBBAA

			Chat chatInstance = Chat.Instance;

			// if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.oocColor))
			// 	return "oocColor";
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.ghostColor))
				return ChatChannel.Ghost.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.commonColor))
				return ChatChannel.Common.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.binaryColor))
				return ChatChannel.Binary.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.supplyColor))
				return ChatChannel.Supply.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.centComColor))
				return ChatChannel.CentComm.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.commandColor))
				return ChatChannel.Command.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.engineeringColor))
				return ChatChannel.Engineering.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.medicalColor))
				return ChatChannel.Medical.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.scienceColor))
				return ChatChannel.Science.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.securityColor))
				return ChatChannel.Security.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.serviceColor))
				return ChatChannel.Service.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.localColor))
				return ChatChannel.Local.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.combatColor))
				return ChatChannel.Combat.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.warningColor))
				return ChatChannel.Warning.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.alienColor))
				return ChatChannel.Alien.ToString();
			if (colorString == ColorUtility.ToHtmlStringRGBA(chatInstance.blobColor))
				return ChatChannel.Blob.ToString();

			return string.Empty;
		}
	}
}
