using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SpriteDataSO : ScriptableObject
{
	public SpriteDataCategory Category = SpriteDataCategory.Null;
}


public enum SpriteDataCategory
{
	Null,
	InHands,
}

[Serializable]
public class DataAndSpritesData {
	
	[SerializeField]
	private SpriteSheetAndData spriteSheetAndData = new SpriteSheetAndData();


	public  SpriteData Data {
		get
		{
			if (data.List.Count == 0)
			{
				data.List.Add(StaticSpriteHandler.CompleteSpriteSetup(spriteSheetAndData));
			}
			return data;
		}
		set
		{
			data = value;
		}
	}

	private SpriteData data = new SpriteData();

	public void SetSpriteSheetAndData(SpriteSheetAndData _spriteSheetAndData) {
		if (_spriteSheetAndData?.Texture != null)
		{ 
			spriteSheetAndData = _spriteSheetAndData;
			data.List.Clear();
			data.List.Add(StaticSpriteHandler.CompleteSpriteSetup(spriteSheetAndData));
		}
	}

}