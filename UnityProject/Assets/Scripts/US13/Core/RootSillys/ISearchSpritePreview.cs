using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISearchSpritePreview
{
	public SpriteDataSO Sprite { get; }
	public Sprite OldSprite { get; }

	public string Name { get; }
}
