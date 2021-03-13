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
    private NetworkedLeanTween tween;

    [FormerlySerializedAs("Effects")]
    [Tooltip("All effects that can be played on this player.")]
    [SerializeField]
    private List<LTEffect> effects;

    private FloatingEffect floatingEffect;
    private RotateEffect rotateEffect;
    private Shake shakeEffect;

    private void Awake()
    {
	    tween = GetComponent<NetworkedLeanTween>();
	    floatingEffect = GetComponent<FloatingEffect>();
	    rotateEffect = GetComponent<RotateEffect>();
	    shakeEffect = GetComponent<Shake>();
	    foreach (var effect in effects)
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
