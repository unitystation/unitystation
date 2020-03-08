using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
	public Sprite[] sprites;

	// Sets the AnimationHandler construct.
	public AnimationHandler(Sprite[] sprites) {
		this.sprites = sprites;
	}

	// Defines new frames to be added to the end of the Animation.
	public AnimationHandler addSprite(AnimationHandler animhand, Sprite sprite) {
		int len = animhand.sprites.Length;
		if (len != 0)
		{
			Array.Resize(ref animhand.sprites, len + 1);
			animhand.sprites[len + 1] = sprite;
		}
		else
		{
			animhand.sprites[0] = sprite;
		}
		return animhand;
	}

	// Removes the latest frame.
	public AnimationHandler removeSprite(AnimationHandler animhand)
	{
		int len = animhand.sprites.Length;
		if (len != 0)
		{
			Array.Resize(ref animhand.sprites, len - 1);
		}
		return animhand;
	}

	// Removes the designated sprite.
	public AnimationHandler extractSprite(AnimationHandler animhand, int removedsprite)
	{
		int len = animhand.sprites.Length;
		AnimationHandler handanim;
		handanim = animhand;
		if (len >= 0 || removedsprite < len)
		{
			for (int i = removedsprite; i < len - 1; i++)
			{
				handanim.sprites[i] = animhand.sprites[i + 1];
			}
			Array.Resize(ref handanim.sprites, len - 1);
		}
		return handanim;
	}

	// Inserts the designated sprite.
	public AnimationHandler insertSprite(AnimationHandler animhand, int insertedSprite, Sprite sprite)
	{
		int len = animhand.sprites.Length;
		AnimationHandler handanim;
		handanim = animhand;
		if (len >= 0 || insertedSprite < len)
		{
			Array.Resize(ref handanim.sprites, len + 1);
			for (int i = len + 1; i > insertedSprite; i--)
			{
				handanim.sprites[i] = animhand.sprites[i - 1];
			}
			handanim.sprites[insertedSprite] = sprite;
		}
		return handanim;
	}

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
