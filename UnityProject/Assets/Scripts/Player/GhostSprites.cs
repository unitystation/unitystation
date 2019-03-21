using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides control of the ghost sprites (the direction the ghost is facing).
/// Similar to PlayerSprites but specifically for ghosts.
/// </summary>
public class GhostSprites : UserControlledSprites
{
	//sprite renderer showing the ghost
	private SpriteRenderer spriteRenderer;
	//ghost sprites for each direction
	private readonly Dictionary<Orientation, Sprite> ghostSprites = new Dictionary<Orientation, Sprite>();

	protected override void Awake()
	{
		base.Awake();
		ghostSprites.Add(Orientation.Down, SpriteManager.PlayerSprites["mob"][268]);
		ghostSprites.Add(Orientation.Up, SpriteManager.PlayerSprites["mob"][269]);
		ghostSprites.Add(Orientation.Right, SpriteManager.PlayerSprites["mob"][270]);
		ghostSprites.Add(Orientation.Left, SpriteManager.PlayerSprites["mob"][271]);

		spriteRenderer = GetComponent<SpriteRenderer>();


	}




	/// <summary>
	/// Locally changes the direction of this player to face the specified direction but doesn't tell the server.
	/// If this is a client, only changes the direction locally and doesn't inform other players / server.
	/// If this is on the server, the direction change will be sent to all clients due to the syncvar.
	///
	/// </summary>
	/// <param name="direction"></param>
	public override void LocalFaceDirection(Orientation direction)
	{

		SetDir(direction);
	}


	/// <summary>
	/// Does nothing if this is the local player.
	///
	/// Invoked when currentDirection syncvar changes. Update the direction of this player ghost to face the specified
	/// direction. However, if this is the local player's ghost nothing is done and we stick with whatever direction we
	/// had already set for them locally (this is to avoid
	/// glitchy changes in facing direction caused by latency in the syncvar).
	/// </summary>
	/// <param name="dir"></param>
	protected override void FaceDirectionSync(Orientation dir)
	{

		if (PlayerManager.LocalPlayer != gameObject)
		{
			currentDirection = dir;
			SetDir(dir);
		}
	}

	/// <summary>
	/// Updates the direction of the ghost if player is a ghost
	/// </summary>
	/// <param name="direction"></param>
	private void SetDir(Orientation direction)
	{
		spriteRenderer.sprite = ghostSprites[direction];
		currentDirection = direction;

	}


	public override void SyncWithServer()
	{
		SetDir(currentDirection);
	}
}
