using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Buckle a player in when they are dragged and dropped while on this object, then unbuckle
/// them when the object is hand-applied to.
/// </summary>
public class BuckleInteract : Interactable<MouseDrop, HandApply>
{
	//may be null
	private OccupiableDirectionalSprite occupiableDirectionalSprite;

	public bool forceUpright;

	private void Start()
	{
		occupiableDirectionalSprite = GetComponent<OccupiableDirectionalSprite>();
		base.Start();
	}

	protected override bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		if (!Validations.ObjectsAtSameTile(interaction.DroppedObject, interaction.TargetObject)) return false;
		if (!Validations.HasComponent<PlayerMove>(interaction.DroppedObject)) return false;
		//if there are any restrained players already here, we can't restrain another one here
		if (MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
			.Any(pm => pm.IsBuckled))
		{
			return false;
		}

		//can't buckle during movement
		var playerSync = interaction.DroppedObject.GetComponent<PlayerSync>();
		if (playerSync.IsMoving) return false;

		//if the player to buckle is currently downed, we cannot buckle if there is another player on the tile
		//(because buckling a player causes the tile to become unpassable, thus a player could end up
		//occupying another player's space)
		var playerMove = interaction.DroppedObject.GetComponent<PlayerMove>();
		var registerPlayer = playerMove.GetComponent<RegisterPlayer>();
		//player to buckle is up, no need to check for other players on the tile
		if (!registerPlayer.IsDown) return true;

		//Player to buckle is down,
		//return false if there are any blocking players on this tile (because if we buckle this player
		//they would become blocking, and we can't have 2 blocking players on the same tile).
		return !MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
			.Any(pm => pm != playerMove && pm.GetComponent<RegisterPlayer>().IsBlocking);
	}

	protected override void ServerPerformInteraction(MouseDrop drop)
	{
		SoundManager.PlayNetworkedAtPos("Click01", drop.TargetObject.WorldPosServer());

		var playerMove = drop.UsedObject.GetComponent<PlayerMove>();
		playerMove.Buckle(gameObject, OnUnbuckle);

		//if this is a directional sprite, we render it in front of the player
		//when they are buckled
		occupiableDirectionalSprite?.RenderOccupied(true);
	}

	protected override bool WillInteractT2(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteractT2(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		//can only do this empty handed
		if (interaction.HandObject != null) return false;
		//can only do this if there is a buckled player here
		return MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
			.Any(pm => pm.IsBuckled);
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		SoundManager.PlayNetworkedAtPos("Click01", interaction.TargetObject.WorldPosServer());

		var playerMoveAtPosition = MatrixManager.GetAt<PlayerMove>(transform.position.CutToInt(), true)?.First(pm => pm.IsBuckled);
		//cannot use the CmdUnrestrain because commands are only allowed to be invoked by local player
		playerMoveAtPosition.Unbuckle();
		//the above will then invoke onunbuckle as it was the callback passed to Restrain
	}

	//delegate invoked from playerMove when they are unrestrained from this
	private void OnUnbuckle()
	{
		occupiableDirectionalSprite?.RenderOccupied(false);
	}
}
