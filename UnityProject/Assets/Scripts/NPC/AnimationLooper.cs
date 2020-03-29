using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Used for mobs with only a constant animation, eg GOD
/// </summary>
public class AnimationLooper : MonoBehaviour
{
	private MobAnimator mobAnimator;
	/// <summary>
	/// The animation you want looped, defaults to right as mobs spawn facing right
	/// </summary>
	public int AnimatorElement = 1;
	/// <summary>
	/// Loop Every .... Seconds
	/// </summary>
	public float LoopEvery = 2f;

    void Start()
    {
		mobAnimator = GetComponent<MobAnimator>();
		Loop();
    }

	private void Loop()
	{
		mobAnimator.ReadyPlayAnimation(AnimatorElement);

		Invoke("Loop", LoopEvery);
	}
}
