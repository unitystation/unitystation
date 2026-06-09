using UnityEngine;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Objects;
using US13.Objects.Directionals;
using US13.Player;
using Util;

namespace pie
{
	public class Pieable : MonoBehaviour, IBumpableObject
	{
		public SpriteHandler spriteHandler;

		public PlayerScript playerScript;

		public void Pie()
		{
			playerScript.RegisterPlayer.ServerStun(2, true, false);
			playerScript.playerSprites.TurnOnPieOverlay();
		}

		public void OnBump(GameObject bumpedBy, GameObject client)
		{
			var creamPie = bumpedBy.GetCachedComponent<CreamPie>();
			if (creamPie != null)
			{
				Pie();
				_ = Despawn.ServerSingle(creamPie.gameObject);
			}
		}
	}
}
