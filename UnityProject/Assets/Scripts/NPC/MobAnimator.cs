using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Basic mob animator using sprite handler.
/// </summary>
public class MobAnimator : MonoBehaviour
{
	private IDictionary<int, Tuple<Sprite[], bool, bool, bool, float, List<int>>> LoadedSprites = new Dictionary<int, Tuple<Sprite[], bool, bool, bool, float, List<int>>>();
	//string is spritepath, list is sprite list

	private int Count = 0;

	public SpriteRenderer spriteRender;

	private NPCDirectionalSprites npcDirectionalSprite;

	private SimpleAnimal simpleAnimal;

	private LivingHealthBehaviour livingHealthBehaviour;

	private Tuple<Sprite[], bool, bool, bool, float, List<int>> NewSprites;

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
		/// Is it a simple animation, does the animation NOT care about direction? IF THIS IS FALSE ALL DIRECTIONS WILL NEED THEIR ANIMATIONS SPECIFIED IN COMPLEX ANIMATION INDEX!
		/// </summary>
		public bool Simple = true;
		/// <summary>
		/// If Simple Animation is true, and this is true the direction will reset to the same direction everytime if it changes during animation, USE FOR MOBS WHICH DONT ONLY HAVE ONE SPRITE DIRECTION.
		/// </summary>
		public bool SimpleResetDirection = false;
		/// <summary>
		/// Death Animation, ignores death checks
		/// </summary>
		public bool IsDeathAnimation = false;
		/// <summary>
		/// Speed of how long to next frame in animation
		/// </summary>
		public float Speed = 0.2f;
		/// <summary>
		/// Animation Index for Complex animation, First is Up, then Right, Down, Left
		/// </summary>
		public List<int> ComplexAnimationIndex = new List<int>();
	}

	public MobAnimation[] Animations;

	public void Awake()
	{
		if (Animations == null) return;

		npcDirectionalSprite = GetComponent<NPCDirectionalSprites>();

		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();

		simpleAnimal = GetComponent<SimpleAnimal>();

		foreach (var AnimationEntry in Animations)
		{
			if (AnimationEntry.Sprites == null) return;
			LoadedSprites.Add(Count, new Tuple<Sprite[], bool, bool, bool, float, List<int>>(AnimationEntry.Sprites, AnimationEntry.Simple, AnimationEntry.SimpleResetDirection, AnimationEntry.IsDeathAnimation, AnimationEntry.Speed, AnimationEntry.ComplexAnimationIndex));
			Count += 1;
		}
	}

	public void ReadyPlayAnimation(int Index = 0)
	{
		if (spriteRender == null) return;

		var spritesToUse = LoadedSprites[Index];

		if (spritesToUse.Item5 == 0) return; //if animation speed if 0

		var animationLength = spritesToUse.Item1.Length;
		if (spritesToUse.Item2 == true)
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
	private IEnumerator SimplePlayAnimation(Tuple<Sprite[], bool, bool, bool, float, List<int>> spritesToUse, int animationLength)
	{
		if (animationLength > 0)
		{
			var startingSprite = spriteRender.sprite;
			var startingDirection = npcDirectionalSprite.CurrentFacingDirection;

			for (int i = 0; i < animationLength; i++)
			{
				if (livingHealthBehaviour.IsDead && spritesToUse.Item4 == false) yield break;//makes sure sprite hasnt changed due to death

				if (npcDirectionalSprite.CurrentFacingDirection != startingDirection && spritesToUse.Item3 == true)// resets direction if changed
				{
					npcDirectionalSprite.ChangeDirection(startingDirection);
				}

				spriteRender.sprite = spritesToUse.Item1[i];// change sprite to new sprite

				yield return WaitFor.Seconds(spritesToUse.Item5 / 2);
			}

			yield return WaitFor.Seconds(spritesToUse.Item5 / 2);

			if (livingHealthBehaviour.IsDead)//another death check
			{
				spriteRender.sprite = simpleAnimal.deadSprite;
				yield break;
			}

			//RESETING SPRITE TO CURRENT DIRECTION SPRITE, animation is over

			if (npcDirectionalSprite.CurrentFacingDirection != startingDirection && spritesToUse.Item3 == true)// resets direction if changed
			{
				npcDirectionalSprite.ChangeDirection(startingDirection);
			}

			spriteRender.sprite = startingSprite;// resets sprite to original sprite
		}
	}

	/// <summary>
	/// COMPLEX animation will attempt animation change if direction is changed, will need at least four animations for each direction specified!
	/// </summary>
	/// <param name="spritesToUse">Tuple containing data</param>
	/// <param name="animationLength">Animation length</param>
	/// <returns></returns>
	private IEnumerator PlayAnimation(Tuple<Sprite[], bool, bool, bool, float, List<int>> spritesToUse, int animationLength, int start = 0)
	{
		if (animationLength > 0 && spritesToUse.Item6.Count == 4)
		{
			var startingSprite = spriteRender.sprite;
			var startingDirection = npcDirectionalSprite.CurrentFacingDirection;
			var animationIndex = spritesToUse.Item6;

			NewSprites = spritesToUse;

			for (int i = start; i < animationLength; i++)
			{
				if (livingHealthBehaviour.IsDead && spritesToUse.Item4 == false) yield break;//makes sure sprite hasnt changed due to death
				if (i > 0 && spriteRender.sprite != NewSprites.Item1[i - 1])
				{
					var currentDirection = npcDirectionalSprite.CurrentFacingDirection;

					if (currentDirection == Vector2.up)
					{
						NewSprites = LoadedSprites[animationIndex[0]]; //sets sprite list based on List int in tuple
					}
					else if (currentDirection == Vector2.right)
					{
						NewSprites = LoadedSprites[animationIndex[1]];
					}
					else if (currentDirection == Vector2.down)
					{
						NewSprites = LoadedSprites[animationIndex[2]];
					}
					else
					{
						NewSprites = LoadedSprites[animationIndex[3]];
					}

					spriteRender.sprite = NewSprites.Item1[i];
				}
				else
				{
					spriteRender.sprite = NewSprites.Item1[i];// change sprite to new sprite
				}

				yield return WaitFor.Seconds(spritesToUse.Item5 / 2);
			}

			yield return WaitFor.Seconds(spritesToUse.Item5 / 2);

			//RESETING SPRITE TO CURRENT DIRECTION SPRITE, animation is over

			if (livingHealthBehaviour.IsDead)//another death check
			{
				spriteRender.sprite = simpleAnimal.deadSprite;
				yield break;
			}

			if (npcDirectionalSprite.CurrentFacingDirection == startingDirection)//If Direction is the same as the direction at start of animation
			{
				spriteRender.sprite = startingSprite;
			}
			else
			{
				var currentDirection = npcDirectionalSprite.CurrentFacingDirection;

				if (currentDirection == Vector2.up)
				{
					spriteRender.sprite = npcDirectionalSprite.upSprite;
				}
				else if (currentDirection == Vector2.right)
				{
					spriteRender.sprite = npcDirectionalSprite.rightSprite;
				}
				else if (currentDirection == Vector2.down)
				{
					spriteRender.sprite = npcDirectionalSprite.downSprite;
				}
				else
				{
					spriteRender.sprite = npcDirectionalSprite.leftSprite;
				}
			}
		}
	}
}
