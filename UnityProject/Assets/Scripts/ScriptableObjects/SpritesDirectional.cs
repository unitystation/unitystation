﻿using System;
using NaughtyAttributes;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "SpritesDirectional", menuName = "ScriptableObjects/Animation/SpritesDirectional",
		order = 0)]
	public class SpritesDirectional : UnityEngine.ScriptableObject
	{
		private const int SIZE = 4;
		[InfoBox("Index: 0 = up, 1 = down, 2 = left, 3 = right", EInfoBoxType.Normal)]
		public Sprite[] sprites = new Sprite[SIZE];
		void OnValidate()
		{
			if (sprites.Length != SIZE)
			{
				Logger.LogWarning("Don't change the 'ints' field's array size!", Category.Sprites);
				Array.Resize(ref sprites, SIZE);
			}
		}

		public Sprite GetSpriteInDirection(OrientationEnum direction)
		{
			if (sprites.Length == 0) return null;
			switch (direction)
			{
				case OrientationEnum.Up:
					return sprites[0];
				case OrientationEnum.Down:
					return sprites[1];
				case OrientationEnum.Left:
					return sprites[2];
				default:
					return sprites[3];
			}
		}

		public static int OrientationIndex(OrientationEnum direction)
		{
			switch (direction)
			{
				case OrientationEnum.Up:
					return 0;
				case OrientationEnum.Down:
					return 1;
				case OrientationEnum.Left:
					return 2;
				default:
					return 3;
			}
		}
	}
}

