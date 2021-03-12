using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// All effects that needed to be played on the player can be called or looked up from here.
/// </summary>
public class PlayerEffectsManager : MonoBehaviour
{
    private NetworkedLeanTween tween;

    [Tooltip("All effects that can be played on this player.")]
    public List<LTEffect> Effects;

	private FloatingEffect floatingEffect;
	private RotateEffect rotateEffect;
	private Shake shakeEffect;
    private void Awake() 
    {
		tween = GetComponent<NetworkedLeanTween>();
		floatingEffect = GetComponent<FloatingEffect>();
		rotateEffect = GetComponent<RotateEffect>();
		shakeEffect = GetComponent<Shake>();
		foreach (var effect in Effects)
		{
			if(effect.tween == null)
			{
				effect.tween = tween;
			}
		}
    }

	private void Update()
	{
		//Checks if the player is floating and animates them up in down if they are.
		if(PlayerManager.PlayerScript.PlayerSync.isFloatingClient == true && floatingEffect.willAnimate == false)
		{
			AnimateFloating();
			return;
		}
		if(PlayerManager.PlayerScript.PlayerSync.isFloatingClient == false && floatingEffect.willAnimate == true)
		{
			AnimateFloating();
		}
	}

	public void AnimateFloating()
	{
		if (floatingEffect.willAnimate)
		{
			floatingEffect.StopFloating();
		}
		else
		{
			floatingEffect.StartFloating();
		}
	}

	public void RotatePlayer(int times, float speed, float degree, bool random)
	{
		rotateEffect.setupEffectvars(times, speed, degree, random);
		rotateEffect.StartAnimation();
	}

	public void ShakePlayer(float duration, float distance, float delay)
	{
		shakeEffect.startShake(duration, distance, delay);
	}
}
