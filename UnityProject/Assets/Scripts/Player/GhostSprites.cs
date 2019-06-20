using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles displaying the ghost sprites.
/// </summary>
[RequireComponent(typeof(Directional))]
public class GhostSprites : MonoBehaviour
{
	//sprite renderer showing the ghost
	private SpriteRenderer spriteRenderer;
	//ghost sprites for each direction
	private readonly Dictionary<Orientation, Sprite> ghostSprites = new Dictionary<Orientation, Sprite>();

	private Directional directional;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		ghostSprites.Add(Orientation.Down, SpriteManager.PlayerSprites["mob"][268]);
		ghostSprites.Add(Orientation.Up, SpriteManager.PlayerSprites["mob"][269]);
		ghostSprites.Add(Orientation.Right, SpriteManager.PlayerSprites["mob"][270]);
		ghostSprites.Add(Orientation.Left, SpriteManager.PlayerSprites["mob"][271]);

		spriteRenderer = GetComponent<SpriteRenderer>();


	}

	private void OnDirectionChange(Orientation direction)
	{
		spriteRenderer.sprite = ghostSprites[direction];
	}
}
