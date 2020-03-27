using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Basic mob animator using sprite handler.
/// </summary>
public class MobAnimator : MonoBehaviour
{
	private IDictionary<int, Tuple<Sprite[], bool, bool>> LoadedSprites = new Dictionary<int, Tuple<Sprite[], bool, bool>>();
	//string is spritepath, list is sprite list

	private int Count = 0;

	public SpriteRenderer spriteRender;

	private NPCDirectionalSprites npcDirectionalSprite;

	private LivingHealthBehaviour livingHealthBehaviour;

	private int CurrentAnimationFrame;

	/// <summary>
	/// The different types of animations this mob has
	/// </summary>
	[Serializable]
	public class MobAnimation
	{
		/// <summary>
		/// Spritepath of the sprites
		/// </summary>
		public Sprite[] Sprites;
		/// <summary>
		/// The number animation frames and which image and order in sprite list
		/// </summary>
		public List<int> AnimationFrames;
		/// <summary>
		/// Does the animation Loop?
		/// </summary>
		public bool Loop = false;
		/// <summary>
		/// Is it a simple animation, does the animation NOT care about direction?, will reset if its direction changes.
		/// </summary>
		public bool Simple = true;
	}

	public MobAnimation[] Animations;

	public void Awake()
	{
		if (Animations == null) return;

		npcDirectionalSprite = GetComponent<NPCDirectionalSprites>();

		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();

		foreach (var AnimationEntry in Animations)
		{
			if (AnimationEntry.Sprites == null) return;
			LoadedSprites.Add(Count, new Tuple<Sprite[], bool, bool>(AnimationEntry.Sprites, AnimationEntry.Loop, AnimationEntry.Simple));
			Count += 1;
		}
	}

	public void ReadyPlayAnimation(int Index = 0, bool StopLoop = false)
	{
		if (spriteRender == null) return;

		var spritesToUse = LoadedSprites[Index];

		var animationLength = spritesToUse.Item1.Length;

		if (spritesToUse.Item2 == true)
		{
			LoopAnimation(spritesToUse, animationLength, StopLoop);
			return;
		}

		if (spritesToUse.Item3 == true && StopLoop == false)
		{
			StartCoroutine(SimplePlayAnimation(spritesToUse, animationLength));
		}
		else
		{
			StartCoroutine(PlayAnimation(spritesToUse, animationLength));
		}
	}

	/// <summary>
	/// Simple Animation doesnt care about direction but will reset if direction is changed.
	/// </summary>
	/// <param name="spritesToUse">Tuple containing data</param>
	/// <param name="Length">Animation length</param>
	/// <returns></returns>
	private IEnumerator SimplePlayAnimation(Tuple<Sprite[], bool, bool> spritesToUse, int Length)
	{
		if (Length > 0)
		{
			var startingSprite = spriteRender.sprite;
			var startingDirection = npcDirectionalSprite.CurrentFacingDirection;

			for (int i = 0; i < Length; i++)
			{
				if (livingHealthBehaviour.IsDead) yield break;//makes sure sprite hasnt changed due to death

				spriteRender.sprite = spritesToUse.Item1[i];// change sprite to new sprite

				if (npcDirectionalSprite.CurrentFacingDirection != startingDirection)// resets direction if changed
				{
					npcDirectionalSprite.ChangeDirection(startingDirection);
				}

				yield return WaitFor.Seconds(0.1f);
			}

			yield return WaitFor.Seconds(0.1f);

			if (livingHealthBehaviour.IsDead) yield break;

			spriteRender.sprite = startingSprite;// resets sprite to original sprite

			if (npcDirectionalSprite.CurrentFacingDirection != startingDirection)// resets direction if changed
			{
				npcDirectionalSprite.ChangeDirection(startingDirection);
			}
		}
	}

	/// <summary>
	/// Complex animation will attempt animation change if direction is changed, will need at least four animations for each direction!
	/// </summary>
	/// <param name="spritesToUse">Tuple containing data</param>
	/// <param name="Length">Animation length</param>
	/// <returns></returns>
	private IEnumerator PlayAnimation(Tuple<Sprite[], bool, bool> spritesToUse, int Length, int start = 0)
	{
		if (Length > 0)
		{
			var startingSprite = spriteRender.sprite;
			var startingDirection = npcDirectionalSprite.CurrentFacingDirection;
			for (int i = start; i < Length; i++)
			{
				if (livingHealthBehaviour.IsDead) yield break;//makes sure sprite hasnt changed due to death
				if (i > 0 && spriteRender.sprite != spritesToUse.Item1[i - 1] && npcDirectionalSprite.CurrentFacingDirection == startingDirection) yield break; 

				spriteRender.sprite = spritesToUse.Item1[i];// change sprite to new sprite

				CurrentAnimationFrame = i;// for use when getting new animation based off direction

				yield return WaitFor.Seconds(0.1f);
			}
			
			yield return WaitFor.Seconds(0.1f);

			if (livingHealthBehaviour.IsDead) yield break; //another death check

			int check = 0;

			foreach (var sprite in spritesToUse.Item1)//makes sure current sprite in the one in the animation.
			{
				if (sprite != startingSprite)
				{
					check += 1;
				}
			}

			if (check == spritesToUse.Item1.Length)// If sprite isnt in animation then needs to find direction and change sprite to that one
			{
				spriteRender.sprite = startingSprite;//change back to sprite before animation
			}
			else
			{

			}
		}
	}

	public void LoopAnimation(Tuple<Sprite[], bool, bool> spritesToUse,int animationLength, bool StopLoop)
	{
		if (StopLoop == true) return;

		if (spritesToUse.Item3 == true)
		{
			StartCoroutine(SimplePlayAnimation(spritesToUse, animationLength));
		}
		else
		{
			StartCoroutine(PlayAnimation(spritesToUse, animationLength));
		}
	}
}
