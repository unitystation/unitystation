using UnityEngine.UI;
using UnityEngine;

public class ChatPlayerNameTooltip : TooltipMonoBehaviour
{
	[SerializeField] private Text playeNameText;

	public override string Tooltip
	{
		get
		{
			if (playeNameText == null)
			{
				Logger.LogWarning("playeNameText is null");
				return string.Empty;
			}

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

			return string.Empty;
		}
	}
}
