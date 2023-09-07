using System;
using Logs;
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
				Loggy.LogWarning("Don't change the 'ints' field's array size!", Category.Sprites);

				Array.Resize(ref sprites, SIZE);
			}
		}

		public Sprite GetSpriteInDirection(OrientationEnum direction)
		{
			if (sprites.Length == 0) return null;
			switch (direction)
			{
				case OrientationEnum.Up_By0:
					return sprites[0];
				case OrientationEnum.Down_By180:
					return sprites[1];
				case OrientationEnum.Left_By90:
					return sprites[2];
				default:
					return sprites[3];
			}
		}

		public static int OrientationIndex(OrientationEnum direction)
		{
			switch (direction)
			{
				case OrientationEnum.Up_By0:
					return 0;
				case OrientationEnum.Down_By180:
					return 1;
				case OrientationEnum.Left_By90:
					return 2;
				default:
					return 3;
			}
		}
	}
}

