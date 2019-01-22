using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
///     Controller for the heart monitor GUI
/// </summary>
public class UI_HeartMonitor : MonoBehaviour
{
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
	private Sprite[] sprites;

	private int spriteStart;
	private bool startMonitoring;
	private float timeWait;
	private int overallHealthCache = 100;

	private void Start()
	{
		sprites = SpriteManager.ScreenUISprites["gen"];
		if (SceneManager.GetActiveScene().name != "Lobby")
		{
			//Game has been started without the lobby scene
			//so start the heart monitor manually
			TryStartMonitor();
		}
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		UpdateManager.Instance.Add(UpdateMe);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	private void OnSceneChange(Scene prev, Scene next)
	{
		if (next.name != "Lobby")
		{
			TryStartMonitor();
		}
		else
		{
			startMonitoring = false;
		}
	}

	private void TryStartMonitor()
	{
		if (!startMonitoring)
		{
			spriteStart = fullHealthStart;
			startMonitoring = true;
		}
	}

	//Managed by UpdateManager
	void UpdateMe()
	{
		if (startMonitoring && PlayerManager.LocalPlayer != null)
		{
			CheckHealth();
			timeWait += Time.deltaTime;
			if (timeWait > 0.05f)
			{
				if (currentSprite != 28)
				{
					pulseImg.sprite = sprites[spriteStart + currentSprite++];
					timeWait = 0f;
				}
				else
				{
					if (timeWait > 2f)
					{
						currentSprite = 0;
						timeWait = 0f;
					}
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

		if (overallHealthCache >= 70 &&
			spriteStart != fullHealthStart)
		{
			SoundManager.Stop("Critstate");
			spriteStart = fullHealthStart;
			pulseImg.sprite = sprites[spriteStart];
			overlayCrits.SetState(OverlayState.normal);
		}
		if (overallHealthCache <= 70 &&
			overallHealthCache > 50 &&
			spriteStart != minorDmgStart)
		{
			SoundManager.Stop("Critstate");
			spriteStart = minorDmgStart;
			pulseImg.sprite = sprites[spriteStart];
			overlayCrits.SetState(OverlayState.injured);
		}
		if (overallHealthCache <= 50 &&
			overallHealthCache > 30 &&
			spriteStart != medDmgStart)
		{
			SoundManager.Stop("Critstate");
			spriteStart = medDmgStart;
			pulseImg.sprite = sprites[spriteStart];
			overlayCrits.SetState(OverlayState.injured);
		}
		if (overallHealthCache <= 30 &&
			overallHealthCache > 0 &&
			spriteStart != mjrDmgStart)
		{
			SoundManager.Stop("Critstate");
			spriteStart = mjrDmgStart;
			pulseImg.sprite = sprites[spriteStart];
			overlayCrits.SetState(OverlayState.injured);
		}
		if (overallHealthCache <= 0 &&
			overallHealthCache < 15 &&
			spriteStart != critStart)
		{
			SoundManager.Play("Critstate");
			spriteStart = critStart;
			pulseImg.sprite = sprites[spriteStart];
			overlayCrits.SetState(OverlayState.unconscious);
		}

		if (overallHealthCache <= -15 &&
			overallHealthCache > -100 &&
			overlayCrits.currentState != OverlayState.crit)
		{
			overlayCrits.SetState(OverlayState.crit);
		}

		if (overallHealthCache == -100 &&
			spriteStart != deathStart)
		{
			SoundManager.Stop("Critstate");
			spriteStart = deathStart;
			pulseImg.sprite = sprites[spriteStart];
			overlayCrits.SetState(OverlayState.death);
		}
	}
}