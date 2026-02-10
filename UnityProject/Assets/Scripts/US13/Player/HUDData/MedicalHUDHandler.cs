using UnityEngine;
using US13.Core.Sprite_Handler;
using Util;

namespace US13.Player.HUDData
{
	public class MedicalHUDHandler : MonoBehaviour
	{

		public SpriteHandler IconSymbol;

		public SpriteHandler BarIcon;

		public void SetVisible(bool Visible)
		{
			IconSymbol.SetActive(Visible);
			BarIcon.SetActive(Visible);
		}

	}
}


