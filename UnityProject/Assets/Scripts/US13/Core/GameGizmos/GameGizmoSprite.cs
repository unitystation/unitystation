using UnityEngine;
using US13.Core.Sprite_Handler;

namespace US13.Core.GameGizmos
{
	public class GameGizmoSprite : GameGizmoTracked
	{
		public SpriteHandler SpriteHandler;
		public void SetUp(GameObject TrackingFrom, Vector3 Position, Color colour, SpriteDataSO Sprite)
		{
			SetUp(Position, TrackingFrom);
			SpriteHandler.SetSpriteSO(Sprite);
			SpriteHandler.SetColor(colour);
		}
	}
}
