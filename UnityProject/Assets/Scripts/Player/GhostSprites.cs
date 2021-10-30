using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Handles displaying the ghost sprites.
/// </summary>
[RequireComponent(typeof(Directional))]
public class GhostSprites : MonoBehaviour, IServerSpawn
{
	//sprite renderer showing the ghost
	private SpriteHandler SpriteHandler;

	public List<SpriteDataSO> GhostSpritesSOs = new List<SpriteDataSO>();

	public List<SpriteDataSO> AdminGhostSpriteSOs = new List<SpriteDataSO>();

	private Directional directional;

	private bool AdminGhost;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
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

	private void OnDirectionChange(Orientation direction)
	{
		if (Orientation.Down == direction)
		{
			SpriteHandler.ChangeSpriteVariant(0, networked:false);
		}
		else if (Orientation.Up == direction)
		{
			SpriteHandler.ChangeSpriteVariant(1, networked:false);
		}
		else if (Orientation.Right == direction)
		{
			SpriteHandler.ChangeSpriteVariant(2, networked:false);
		}
		else
		{
			SpriteHandler.ChangeSpriteVariant(3, networked:false);
		}
	}
}
