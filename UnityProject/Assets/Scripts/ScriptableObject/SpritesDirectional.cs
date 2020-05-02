using System;
using UnityEngine;


[CreateAssetMenu(fileName = "SpritesDirectional", menuName = "ScriptableObjects/Animation/SpritesDirectional",
	order = 0)]
public class SpritesDirectional : UnityEngine.ScriptableObject
{
	private const int SIZE = 4;
	public Sprite[] sprites = new Sprite[SIZE];
	void OnValidate()
	{
		if (sprites.Length != SIZE)
		{
			Debug.LogWarning("Don't change the 'ints' field's array size!");
			Array.Resize(ref sprites, SIZE);
		}
	}
	public Sprite GetSpriteInDirection(OrientationEnum direction)
	{
		if (sprites.Length == 0) return null;
		switch (direction)
		{
			case OrientationEnum.Down:
				return sprites[0];
			case OrientationEnum.Left:
				return sprites[3];
			case OrientationEnum.Up:
				return sprites[2];
			default:
				return sprites[1];
		}
	}
}

