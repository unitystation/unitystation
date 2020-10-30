using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class ControlWalkRun : TooltipMonoBehaviour
{
	private Image image;
	public Sprite[] runWalkSprites;
	public override string Tooltip => "run/walk toggle";

	public bool running { get; private set; } = true;

	private void Start()
	{
		image = GetComponent<Image>();
	}

	/*
	* Button OnClick methods
	*/

	public void RunWalk()
	{
		Logger.Log("RunWalk Button", Category.UI);

		// JESTE_R
		SoundManager.Play(SingletonSOSounds.Instance.Click01);

		if (!running)
		{
			running = true;
			image.sprite = runWalkSprites[1];
		}
		else
		{
			running = false;
			image.sprite = runWalkSprites[0];
		}
	}
}
