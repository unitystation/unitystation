using System;
using Light2D;
using UnityEngine;

/// <summary>
/// Simple light glitch behaviour.
/// Used to display light dynamics for new light system.
/// </summary>
public class LightGlitch : MonoBehaviour
{
	private LightSprite mLightSprite;
	private float mTimeSinceLastGlitch;
	private float GlitchTime;
	private bool mGlitchActive;
	private float mTimeGlitchActive;
	private float GlitchDuration;
	private float mTimeSinceLastGlitchEffect;
	private bool mGlitchState;
	private Color mOriginalColor;
	private Vector3 mOriginalPosition;

	private bool glitchState
	{
		get
		{
			return mGlitchState;
		}
		set
		{
			mGlitchState = value;

			if (value)
			{
				mLightSprite.Color = mOriginalColor - new Color(0.2f, 0.2f, 0);
				mLightSprite.transform.localPosition = mOriginalPosition + new Vector3(0.3f,0.3f,0);
			}
			else
			{
				mLightSprite.Color = mOriginalColor;
				mLightSprite.transform.localPosition = mOriginalPosition;
			}
		}
	}

	public static LightGlitch Create(GameObject iGameObject)
	{
		return iGameObject.AddComponent<LightGlitch>();
	}

	private void Start()
	{
		mLightSprite = gameObject.GetComponent<LightSprite>();

		if (mLightSprite == null)
		{
			mLightSprite = gameObject.GetComponentInChildren<LightSprite>(true);
		}

		mOriginalColor = mLightSprite.Color;
		mOriginalPosition = mLightSprite.transform.localPosition;

		GlitchTime = UnityEngine.Random.Range(0.5f, 3f);
		GlitchDuration = UnityEngine.Random.Range(0.2f, 1f);
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void UpdateMe()
	{
		if (mLightSprite == null)
			return;

		if (mGlitchActive)
		{
			mTimeGlitchActive += Time.deltaTime;

			mTimeSinceLastGlitchEffect += Time.deltaTime;

			if (mTimeSinceLastGlitchEffect > 0.075f)
			{
				mTimeSinceLastGlitchEffect = 0;

				glitchState = !glitchState;
			}

			if (mTimeGlitchActive >= GlitchDuration)
			{
				mGlitchActive = false;
				glitchState = false;
			}
		}
		else
		{
			mTimeSinceLastGlitch += Time.deltaTime;

			if (mTimeSinceLastGlitch >= GlitchTime)
			{
				Glitch();
			}
		}
	}

	private void Glitch()
	{
		mTimeSinceLastGlitch = 0;
		mTimeGlitchActive = 0;
		mGlitchActive = true;
	}
}