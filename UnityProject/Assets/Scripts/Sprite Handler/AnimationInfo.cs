using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationInfo : MonoBehaviour
{
	public Sprite[] sprites;
	public int FramesPerSecond;

	#region Server

	// Sets the AnimationInfo construct.
	public AnimationInfo(Sprite[] sprites, int FramesPerSecond) {
		this.sprites = sprites;
		this.FramesPerSecond = FramesPerSecond;
	}

	// Defines new frames to be added to the end of the AnimationInfo.
	public AnimationInfo addSprite(AnimationInfo animhand, Sprite sprite)
	{
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
	public AnimationInfo removeSprite(AnimationInfo animhand)
	{
		int len = animhand.sprites.Length;
		if (len != 0)
		{
			Array.Resize(ref animhand.sprites, len - 1);
		}
		return animhand;
	}
	// Removes the designated sprite.
	public AnimationInfo extractSprite(AnimationInfo animhand, int removedsprite)
	{
		int len = animhand.sprites.Length;
		AnimationInfo handanim;
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
	public AnimationInfo insertSprite(AnimationInfo animhand, int insertedSprite, Sprite sprite)
	{
		int len = animhand.sprites.Length;
		AnimationInfo handanim;
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
	// Sets the ammount of frames played per second.
	public AnimationInfo setFPS(AnimationInfo animhand, int FPS)
	{
		animhand.FramesPerSecond = FPS;
		return animhand;
	}

	#endregion

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
