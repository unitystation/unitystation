using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior common to all wall mounts.
///
/// Adds a WallmountSpriteBehavior to all child objects that have SpriteRenderers. Facing / visibility checking is handled in
/// there. See <see cref="WallmountSpriteBehavior"/>
/// </summary>
public class WallmountBehavior : MonoBehaviour {
	private void Start()
	{
		//add the behavior to all child spriterenderers
		foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
		{
			renderer.gameObject.AddComponent<WallmountSpriteBehavior>();
		}
	}
}
