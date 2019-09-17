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

	public SpriteSheetAndData GhostSpritese;

	private Directional directional;

	protected void Awake()
	{
		directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChange);
		ghostSprites.Add(Orientation.Down, GhostSpritese.Sprites[0]);
		ghostSprites.Add(Orientation.Up, GhostSpritese.Sprites[1]);
		ghostSprites.Add(Orientation.Right, GhostSpritese.Sprites[2]);
		ghostSprites.Add(Orientation.Left, GhostSpritese.Sprites[3]);

		spriteRenderer = GetComponent<SpriteRenderer>();


	}

	private void OnDirectionChange(Orientation direction)
	{
		spriteRenderer.sprite = ghostSprites[direction];
	}
}
