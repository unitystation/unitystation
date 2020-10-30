using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
///     Controller for the heart monitor GUI
/// </summary>
public class UI_HeartMonitor : TooltipMonoBehaviour
{
	public override string Tooltip => "health";
	[SerializeField] private AddressableAudioSource Critstate = null;
	public int critStart;
	private int currentSprite = 0;
	public int deathStart;

	[Header("Start of sprite positions for anim")] public int fullHealthStart;
	public int medDmgStart;
	public int minorDmgStart;
	public int mjrDmgStart;

	//FIXME doing overlayCrit update based off heart monitor for time being
	public OverlayCrits overlayCrits;

	public Image pulseImg;

	[SerializeField]
	public List<Spritelist> StatesSprites;
	private int CurrentSpriteSet = 0;
	private float timeWait;
	private float overallHealthCache = 100;


	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void OnSceneChange(Scene prev, Scene next)
	{
		// Ensure crit overlay is reset to normal.
		overlayCrits.SetState(OverlayState.normal);
	}

	//Managed by UpdateManager
	void UpdateMe()
	{
		if (PlayerManager.LocalPlayer == null || PlayerManager.LocalPlayerScript.IsGhost) return;

		CheckHealth();
		timeWait += Time.deltaTime;
		if (timeWait > 0.05f)
		{
			if (currentSprite != 27)
			{
				pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
				currentSprite++;
				timeWait = 0f;
			}
			else
			{
				pulseImg.sprite = StatesSprites[CurrentSpriteSet].SP[currentSprite];
				if (timeWait > 2f)
				{
					currentSprite = 0;
					timeWait = 0f;
				}
			}
		}
	}

	private void CheckHealth()
	{
		if (PlayerManager.LocalPlayerScript.playerHealth.OverallHealth == overallHealthCache)
		{
			return;
		}
		overallHealthCache = PlayerManager.LocalPlayerScript.playerHealth.OverallHealth;

		if (overallHealthCache >= 100)
		{
			SoundManager.Stop("Critstate");
			CurrentSpriteSet = 0;
			pulseImg.sprite = StatesSprites[0].SP[currentSprite];
			overlayCrits.SetState(OverlayState.normal);
		}
		if (overallHealthCache >= 70)
		{
			CurrentSpriteSet = 1;
			SoundManager.Stop("Critstate");
			pulseImg.sprite = StatesSprites[1].SP[currentSprite];
			overlayCrits.SetState(OverlayState.normal);
		}
		if (overallHealthCache <= 70 &&
			overallHealthCache > 50)
		{
			SoundManager.Stop("Critstate");
			CurrentSpriteSet = 2;
			pulseImg.sprite = StatesSprites[2].SP[currentSprite];
			overlayCrits.SetState(OverlayState.injured);
		}
		if (overallHealthCache <= 50 &&
			overallHealthCache > 30)
		{
			SoundManager.Stop("Critstate");
			CurrentSpriteSet = 3;
			pulseImg.sprite = StatesSprites[3].SP[currentSprite];
			overlayCrits.SetState(OverlayState.injured);
		}
		if (overallHealthCache <= 30 &&
			overallHealthCache > 0)
		{
			SoundManager.Stop("Critstate");
			CurrentSpriteSet = 4;
			pulseImg.sprite = StatesSprites[4].SP[currentSprite];
			overlayCrits.SetState(OverlayState.injured);
		}
		if (overallHealthCache <= 0 &&
			overallHealthCache < 15)
		{

			// JESTE_R
			SoundManager.Play(Critstate,"Critstate");
			CurrentSpriteSet = 5;
			pulseImg.sprite = StatesSprites[5].SP[currentSprite];
			overlayCrits.SetState(OverlayState.unconscious);
		}

		if (overallHealthCache <= -15 &&
			overallHealthCache > -100)
		{
			CurrentSpriteSet = 6;
			pulseImg.sprite = StatesSprites[6].SP[currentSprite];
			overlayCrits.SetState(OverlayState.crit);
		}

		if (overallHealthCache <= -100)
		{
			SoundManager.Stop("Critstate");
			CurrentSpriteSet = 7;
			pulseImg.sprite = StatesSprites[7].SP[currentSprite];
			overlayCrits.SetState(OverlayState.death);
		}
	}
}
