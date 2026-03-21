using System;
using UnityEngine;
using US13.Core;
using US13.Core.Sprite_Handler;
using US13.Managers.MatrixManager;
using US13.Managers.UpdateManager;
using US13.Systems.Ai;
using US13.Tilemaps.Utils;
using Util;

public class AIDetector : MonoBehaviour
{
	public SpriteHandler  SpriteHandler;

	public static LayerTypeSelection ObstructionLayerMask => LayerTypeSelection.Walls;

	public void OnEnable()
	{
		UpdateManager.Add(UpdateMe, 0.5f);
	}

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE,UpdateMe);
	}

	public void UpdateMe()
	{
		var AIs = ComponentsTracker<AiPlayer>.GetAllNearbyTypesToLocation(gameObject.AssumedWorldPosServer(), 12);
		if (AIs ==null) return;
		if (AIs.Count > 0)
		{
			bool LineOfSight = false;

			foreach (var AI in AIs)
			{
				var line = MatrixManager.Linecast(
					transform.position,
					ObstructionLayerMask,
					null,
					AI.fovFollowCamera.gameObject.AssumedWorldPosServer());
				if (line.ItHit == false)
				{
					LineOfSight = true;
					break;
				}
			}


			if (LineOfSight)
			{
				SpriteHandler.SetCatalogueIndexSprite(2);
			}
			else
			{
				SpriteHandler.SetCatalogueIndexSprite(1);
			}
		}
		else
		{
			SpriteHandler.SetCatalogueIndexSprite(0);
		}

	}
}
