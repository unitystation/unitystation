﻿	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using Light2D;
	using UnityEngine;

	// Note: Judging the "lighting" sprite sheet it seems that light source can have many disabled states.
	// At this point i just want to do a basic setup for an obvious extension, so only On / Off states are actually implemented 
	// and for other states is just a state and sprite assignment.
	internal enum LightState
	{
		None = 0,

		On,
		Off,

		// Placeholder states, i assume naming would change.
		MissingBulb,
		Dirty,
		Broken,

		TypeCount,
	}

	public class LightSource : ObjectTrigger
	{
		private const LightState InitialState = LightState.On;

		private readonly Dictionary<LightState, Sprite> mSpriteDictionary = new Dictionary<LightState, Sprite>((int)LightState.TypeCount);

		private GameObject mLightRendererObject;
		private LightState mState;
		private SpriteRenderer Renderer;
		private bool tempStateCache;

		// For network sync reliability.
		private bool waitToCheckState;

		private LightState State
		{
			get
			{
				return mState;
			}

			set
			{
				if (mState == value)
					return;

				mState = value;

				OnStateChange(value);
			}
		}
	
		public override void Trigger(bool iState)
		{
			// Leo Note: Some sync magic happening here. Decided not to touch it.
			tempStateCache = iState;

			if (waitToCheckState)
			{
				return;
			}

			if (Renderer == null)
			{
				waitToCheckState = true;
				StartCoroutine(WaitToTryAgain());
				return;
			}
			else
			{
				State = iState ? LightState.On : LightState.Off;
			}
		}

		private void OnStateChange(LightState iValue)
		{
			// Assign state appropriate sprite to the LightSourceObject.
			if (mSpriteDictionary.ContainsKey(iValue))
			{
				Renderer.sprite = mSpriteDictionary[iValue];
			}
			else if (mSpriteDictionary.Any())
			{
				Renderer.sprite = mSpriteDictionary.Values.First();
			}

			// Switch Light renderer.
			if (mLightRendererObject != null)
				mLightRendererObject.SetActive(iValue == LightState.On);
		}

		private void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();

			// Slight color variance.
			Color _color = new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f); //+ UnityEngine.Random.ColorHSV() * 0.3f;

			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, _color, 12);

			State = InitialState;

			ExtractLightSprites();
		}

		private void ExtractLightSprites()
		{
			// Reimplementation of sprite location on atlas.

			// Note: It is quite magical and really should not be done like this:
			// It takes an assigned sprite name, parses its index, adds 4 to it and takes resulting sprite from the sheet.
			// There is a bold assumption that sprite sheets associated with states are spaced 4 indexes between, and that nobody has changed any sprite names.
			// My reimplementation just grabs more sprites for associated states.

			const int SheetSpacing = 4;

			var _assignedSprite = Renderer.sprite;

			if (_assignedSprite == null)
			{
				UnityEngine.Debug.LogError("LightSource: Unable to extract light source state sprites from SpriteSheet. Operation require Renderer.sprite to be assigned in inspector.");
				return;
			}

			// Try to parse base sprite index.
			string[] _splitedName = _assignedSprite.name.Split('_');
			var _spriteSheet = SpriteManager.LightSprites["lights"];

			int _baseIndex;
			if (_spriteSheet != null && _splitedName.Length == 2 && int.TryParse(_splitedName[1], out _baseIndex))
			{
				Func<int, Sprite> ExtractSprite = delegate(int iIndex)
					{
						if (iIndex >= 0 && iIndex < _spriteSheet.Length)
							return _spriteSheet[iIndex];

						return null;
					};

				// Extract sprites from sprite sheet based on spacing from base index.
				mSpriteDictionary.Add(LightState.On, _assignedSprite);
				mSpriteDictionary.Add(LightState.Off, ExtractSprite(_baseIndex + SheetSpacing));
				mSpriteDictionary.Add(LightState.MissingBulb, ExtractSprite(_baseIndex + (SheetSpacing * 2)));
				mSpriteDictionary.Add(LightState.Dirty, ExtractSprite(_baseIndex + (SheetSpacing * 3)));
				mSpriteDictionary.Add(LightState.Broken, ExtractSprite(_baseIndex + (SheetSpacing * 4)));
			}
			else
			{
				mSpriteDictionary.Add(LightState.On, _assignedSprite);
			}
		}

		// Handle sync failure.
		private IEnumerator WaitToTryAgain()
		{
			yield return new WaitForSeconds(0.2f);
			if (Renderer == null)
			{
				Renderer = GetComponentInChildren<SpriteRenderer>();
				if (Renderer != null)
				{
					State = tempStateCache ? LightState.On : LightState.Off;
					if (mLightRendererObject != null)
					{
						mLightRendererObject.SetActive(tempStateCache);
					}
				}
				else
				{
					Logger.LogWarning("LightSource still failing Renderer sync", Category.Lighting);
				}
			}
			else
			{
				State = tempStateCache ? LightState.On : LightState.Off;
				if (mLightRendererObject != null)
				{
					mLightRendererObject.SetActive(tempStateCache);
				}
			}
			waitToCheckState = false;
		}
	}