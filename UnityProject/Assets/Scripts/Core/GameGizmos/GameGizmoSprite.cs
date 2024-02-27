using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
