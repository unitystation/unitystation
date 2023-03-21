using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCubeSprite : MonoBehaviour
{
	public List<SpriteRenderer> Handlers = new List<SpriteRenderer>();

	public void SetSprite(Sprite Sprite)
	{
		foreach (var Handler in Handlers)
		{
			Handler.sprite = Sprite;
		}
	}

}
