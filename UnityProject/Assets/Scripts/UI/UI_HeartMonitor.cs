using System.Collections;
using PlayGroup;
using Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	///     Controller for the heart monitor GUI
	/// </summary>
	public class UI_HeartMonitor : MonoBehaviour
	{
		public int critStart;
		private int currentSprite;
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
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
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
				StartCoroutine(MonitorHealth());
			}
		}

		private IEnumerator MonitorHealth()
		{
			currentSprite = 0; //28 length for monitor anim
			while (startMonitoring)
			{
				while (PlayerManager.LocalPlayer == null)
				{
					yield return new WaitForSeconds(1f);
				}
				pulseImg.sprite = sprites[spriteStart + currentSprite++];
				while (currentSprite == 28)
				{
					yield return new WaitForSeconds(0.05f);
					timeWait += Time.deltaTime;
					if (timeWait >= 2f)
					{
						timeWait = 0f;
						currentSprite = 0;
					}
				}

				yield return new WaitForSeconds(0.05f);
			}

			yield return new WaitForEndOfFrame();
		}

		public void DetermineDisplay(PlayerHealthUI pHealthUI, int curHealth)
		{
			if (pHealthUI == null)
			{
				return;
			}
			if (curHealth <= -1 && spriteStart == deathStart)
			{
				return; //Ensure that messages are not spammed when there is no more health to go
			}

			CheckHealth(curHealth);
		}

		private void CheckHealth(int cHealth)
		{
			//PlayGroup.PlayerManager.PlayerScript.playerNetworkActions.CmdSendAlertMessage("mHealth: " + cHealth, true);
			//Debug.Log("PlayerHealth: " + PlayGroup.PlayerManager.PlayerScript.playerHealth.Health);
			if (cHealth >= 90
			    && spriteStart != fullHealthStart)
			{
				SoundManager.Stop("Critstate");
				spriteStart = fullHealthStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.normal);
			}
			if (cHealth < 90
			    && cHealth > 80
			    && spriteStart != minorDmgStart)
			{
				SoundManager.Stop("Critstate");
				spriteStart = minorDmgStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.injured);
			}
			if (cHealth < 80
			    && cHealth > 50
			    && spriteStart != medDmgStart)
			{
				SoundManager.Stop("Critstate");
				spriteStart = medDmgStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.injured);
			}
			if (cHealth < 50
			    && cHealth > 30
			    && spriteStart != mjrDmgStart)
			{
				SoundManager.Stop("Critstate");
				spriteStart = mjrDmgStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.injured);
			}
			if (cHealth < 30
			    && cHealth > 0
			    && spriteStart != critStart)
			{
				SoundManager.Play("Critstate");
				spriteStart = critStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.unconscious);
			}

			if (cHealth < 15
			    && cHealth > 0
			    && overlayCrits.currentState != OverlayState.crit)
			{
				overlayCrits.SetState(OverlayState.crit);
			}

			if (cHealth <= 0
			    && spriteStart != deathStart)
			{
				SoundManager.Stop("Critstate");
				spriteStart = deathStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.death);
			}
		}
	}
}