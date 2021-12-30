using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Objects.Construction;

using UnityEngine;
using Chemistry.Components;
using Core.Directionals;

//[RequireComponent(typeof(DirectionalSpriteV2))]
public class MakesFootPrints : MonoBehaviour, IServerInventoryMove
{
	public ReagentContainer spillContents;
	private PlayerScript me;
	private Vector3Int oldPosition;
	public SpriteHandler spriteHandler;
	public GameObject shoeprint;
	private DirectionalSpriteV2 directionalSprite;

	#region Lifecycle
	public void Awake()
	{
		//spillContents = gameObject.GetComponent<ReagentContainer>();
		oldPosition = gameObject.AssumedWorldPosServer().RoundToInt();
		me = GetComponentInParent<PlayerScript>();
		Debug.Log(me);
	}
	#endregion Lifecycle

	public void OnInventoryMoveServer(InventoryMove info)
	{
		me = info.ToRootPlayer.PlayerScript;

	}
	// Update is called once per frame
	void Update()
    {
		if (spillContents.ReagentMixTotal > 0f)
		{
			//if being worn

			Vector3Int currentPosition = gameObject.AssumedWorldPosServer().RoundToInt();
			if(currentPosition != oldPosition && !MatrixManager.IsSpaceAt(gameObject.AssumedWorldPosServer().RoundToInt(), true))
			{
				//Debug.Log(me.CurrentDirection.AsEnum());
				if (MatrixManager.GetAt<FloorDecal>(currentPosition, isServer: true).Any(decal => decal.isFootprint))
				{
					var decal = MatrixManager.GetAt<FloorDecal>(currentPosition, true);
					Debug.Log("Im leaving a footprint");
				}
					MatrixManager.ReagentReact(spillContents.TakeReagents(0.1f), gameObject.AssumedWorldPosServer().RoundToInt(),null,true, me.CurrentDirection);
				//max.floorInteract(reagents,pos,mat,direction,spritePrefab, alpha
				oldPosition = currentPosition;
				//Debug.Log(me.CurrentDirection.AsEnum());
				
			}

		}
	}
	
}
