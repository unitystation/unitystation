using System;
using UnityEngine;
using US13.UI.Core;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	[Serializable]
	public class Handcuff : TooltipMonoBehaviour
	{
		// The image that should be displayed when the player is handcuffed
		[Tooltip("The image that should be displayed when the player is handcuffed")]
		[SerializeField]
		private Sprite sprite = null;

		public Sprite HandcuffSprite => sprite;
	}
}
