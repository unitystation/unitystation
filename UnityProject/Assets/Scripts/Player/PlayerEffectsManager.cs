using UnityEngine;
using System.Collections.Generic;

public class PlayerEffectsManager : MonoBehaviour
{
    private NetworkedLeanTween tween;

    [Tooltip("All effects that can be played on this player.")]
    public List<LTEffect> Effects;

	private FloatingEffect floatingEffect;
    private void Awake() 
    {
		tween = GetComponent<NetworkedLeanTween>();
		floatingEffect = GetComponent<FloatingEffect>();
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
			floatingEffect.stopFloating();
		}
		else
		{
			floatingEffect.startFloating();
		}
	}
}
