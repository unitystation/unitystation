using UnityEngine;
using US13.Core.Sprite_Handler;
using Util;

namespace US13.Player.HUDData
{
	public class SecurityHUDHandler : MonoBehaviour
	{

		public SpriteHandler MindShieldImplant;

		public SpriteHandler RoleIcon;

		public SpriteHandler StatusIcon;


		public void SetVisible(bool Visible)
		{
			MindShieldImplant.SetActive(Visible);
			RoleIcon.SetActive(Visible);
			StatusIcon.SetActive(Visible);
		}


	}
}
