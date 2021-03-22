using UnityEngine;
using System.Collections.Generic;
using Effects;
using UnityEngine.Serialization;
using Util;

/// <summary>
/// All effects that needed to be played on the player can be called or looked up from here.
/// </summary>
public class PlayerEffectsManager : MonoBehaviour
{
    private FloatingEffect floatingEffect;
    private RotateEffect rotateEffect;
    private Shake shakeEffect;

    private void Awake()
    {
	    floatingEffect = GetComponent<FloatingEffect>();
	    rotateEffect = GetComponent<RotateEffect>();
	    shakeEffect = GetComponent<Shake>();
    }

    private void Update()
    {
	    if (PlayerManager.PlayerScript.OrNull()?.PlayerSync == null ) return;
	    //Checks if the player is floating and animates them up in down if they are.
	    if(PlayerManager.PlayerScript.PlayerSync.isFloatingClient && floatingEffect.WillAnimate == false)
	    {
		    AnimateFloating();
		    return;
	    }
	    if(PlayerManager.PlayerScript.PlayerSync.isFloatingClient == false && floatingEffect.WillAnimate)
	    {
		    AnimateFloating();
	    }
    }

    private void AnimateFloating()
    {
	    if (floatingEffect.WillAnimate)
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
	    rotateEffect.SetupEffectvars(times, speed, degree, random);
	    rotateEffect.StartAnimation();
    }

    public void ShakePlayer(float duration, float distance, float delay)
    {
	    shakeEffect.StartShake(duration, distance, delay);
    }
}
