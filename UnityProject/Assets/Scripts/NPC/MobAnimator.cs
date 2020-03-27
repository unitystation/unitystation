using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Basic mob animator using sprite handler.
/// </summary>
public class MobAnimator : MonoBehaviour
{
	private IDictionary<int, Tuple<Sprite[], bool, bool, bool, float>> LoadedSprites = new Dictionary<int, Tuple<Sprite[], bool, bool, bool, float>>();
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
		/// Is it a simple animation, does the animation NOT care about direction? IF THIS IS FALSE ALL DIRECTIONS WILL NEED THEIR OWN ANIMATIONS!
		/// </summary>
		public bool Simple = true;
		/// <summary>
		/// If Simple Animation is true, and this is true the direction will reset to the same direction everytime if it changes during animation, USE FOR MOBS WHICH ONLY HAVE ONE SPRITE DIRECTION.
		/// </summary>
		public bool SimpleResetDirection = false;
		/// <summary>
		/// Speed of how long to next frame in animation
		/// </summary>
		public float Speed = 0.2f;
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
			LoadedSprites.Add(Count, new Tuple<Sprite[], bool, bool, bool, float>(AnimationEntry.Sprites, AnimationEntry.Loop, AnimationEntry.Simple, AnimationEntry.SimpleResetDirection, AnimationEntry.Speed));
			Count += 1;
		}
	}

	public void ReadyPlayAnimation(int Index = 0, bool StopLoop = false)
	{
		if (spriteRender == null) return;

		var spritesToUse = LoadedSprites[Index];

		if (spritesToUse.Item5 == 0) return; //if animation speed if 0

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
	/// <param name="animationLength">Animation length</param>
	/// <returns></returns>
	private IEnumerator SimplePlayAnimation(Tuple<Sprite[], bool, bool, bool, float> spritesToUse, int animationLength)
	{
		if (animationLength > 0)
		{
			var startingSprite = spriteRender.sprite;
			var startingDirection = npcDirectionalSprite.CurrentFacingDirection;

			for (int i = 0; i < animationLength; i++)
			{
				if (livingHealthBehaviour.IsDead) yield break;//makes sure sprite hasnt changed due to death

				spriteRender.sprite = spritesToUse.Item1[i];// change sprite to new sprite

				if (npcDirectionalSprite.CurrentFacingDirection != startingDirection && spritesToUse.Item4 == true)// resets direction if changed
				{
					npcDirectionalSprite.ChangeDirection(startingDirection);
				}

				yield return WaitFor.Seconds(spritesToUse.Item5 / 2);
			}

			yield return WaitFor.Seconds(spritesToUse.Item5 / 2);

			if (livingHealthBehaviour.IsDead) yield break;

			spriteRender.sprite = startingSprite;// resets sprite to original sprite

			if (npcDirectionalSprite.CurrentFacingDirection != startingDirection && spritesToUse.Item4 == true)// resets direction if changed
			{
				npcDirectionalSprite.ChangeDirection(startingDirection);
			}
		}
	}

	/// <summary>
	/// Complex animation will attempt animation change if direction is changed, will need at least four animations for each direction specified!
	/// </summary>
	/// <param name="spritesToUse">Tuple containing data</param>
	/// <param name="animationLength">Animation length</param>
	/// <returns></returns>
	private IEnumerator PlayAnimation(Tuple<Sprite[], bool, bool, bool, float> spritesToUse, int animationLength, int start = 0)
	{
		if (animationLength > 0)
		{
			var startingSprite = spriteRender.sprite;
			var startingDirection = npcDirectionalSprite.CurrentFacingDirection;
			for (int i = start; i < animationLength; i++)
			{
				if (livingHealthBehaviour.IsDead) yield break;//makes sure sprite hasnt changed due to death
				if (i > 0 && spriteRender.sprite != spritesToUse.Item1[i - 1] && npcDirectionalSprite.CurrentFacingDirection == startingDirection) yield break; 

				spriteRender.sprite = spritesToUse.Item1[i];// change sprite to new sprite

				CurrentAnimationFrame = i;// for use when getting new animation based off direction

				yield return WaitFor.Seconds(spritesToUse.Item5 / 2);
			}
			
			yield return WaitFor.Seconds(spritesToUse.Item5 / 2);

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

	public void LoopAnimation(Tuple<Sprite[], bool, bool, bool, float> spritesToUse,int animationLength, bool StopLoop)
	{
		var stopLoop = StopLoop;

		if (StopLoop == true || stopLoop == true) return;

		if (spritesToUse.Item3 == true)
		{
			StartCoroutine(SimplePlayAnimation(spritesToUse, animationLength));
		}
		else
		{
			StartCoroutine(PlayAnimation(spritesToUse, animationLength));
		}

		Invoke("LoopAnimation", 2f);
	}
}
