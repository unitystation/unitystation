using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
	#region Server
	private IEnumerator coroutine;

	public void Animator(AnimationInfo anim, SpriteRenderer spriteRenderer)
	{
		int len = anim.sprites.Length;
		int FPS = anim.FramesPerSecond;

		if (len == 0)
		{
			spriteRenderer.sprite = anim.sprites[0];
		}
		else if (len > 0)
		{
			for (int i = 0; i > len; i++)
			{
				coroutine = WaitAndShow(1 / FPS, spriteRenderer, anim, i);
				StartCoroutine(coroutine);
			}
		}
	}

	private IEnumerator WaitAndShow(float waitTime, SpriteRenderer sRenderer, AnimationInfo anim, int i)
	{
		yield return new WaitForSeconds(waitTime);
		sRenderer.sprite = anim.sprites[i];
	}

	#endregion

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
