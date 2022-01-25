using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Handles displaying the ghost sprites.
/// </summary>
[RequireComponent(typeof(Rotatable))]
public class GhostSprites : MonoBehaviour, IServerSpawn
{
	//sprite renderer showing the ghost
	private SpriteHandler SpriteHandler;

	public List<SpriteDataSO> GhostSpritesSOs = new List<SpriteDataSO>();

	public List<SpriteDataSO> AdminGhostSpriteSOs = new List<SpriteDataSO>();

	private Rotatable rotatable;

	private bool AdminGhost;

	protected void Awake()
	{
		rotatable = GetComponent<Rotatable>();
		rotatable.OnRotationChange.AddListener(OnDirectionChange);
		SpriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (AdminGhost)
		{
			SpriteHandler.SetSpriteSO(AdminGhostSpriteSOs.PickRandom());
		}
		else
		{
			SpriteHandler.SetSpriteSO(GhostSpritesSOs.PickRandom());
		}
	}

	public void SetAdminGhost()
	{
		AdminGhost = true;
	}

	private void OnDirectionChange(OrientationEnum direction)
	{
		if (OrientationEnum.Down_By180 == direction)
		{
			SpriteHandler.ChangeSpriteVariant(0, networked:false);
		}
		else if (OrientationEnum.Up_By0 == direction)
		{
			SpriteHandler.ChangeSpriteVariant(1, networked:false);
		}
		else if (OrientationEnum.Right_By90 == direction)
		{
			SpriteHandler.ChangeSpriteVariant(2, networked:false);
		}
		else
		{
			SpriteHandler.ChangeSpriteVariant(3, networked:false);
		}
	}
}
