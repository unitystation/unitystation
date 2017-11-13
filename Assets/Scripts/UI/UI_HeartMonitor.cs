using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sprites;
using UnityEngine.SceneManagement;

namespace UI
{
	/// <summary>
	/// Controller for the heart monitor GUI
	/// </summary>
	public class UI_HeartMonitor : MonoBehaviour
	{
		public Image pulseImg;
		private Sprite[] sprites;
		private bool startMonitoring;

		[Header("Start of sprite positions for anim")]
		public int fullHealthStart;
		public int minorDmgStart;
		public int medDmgStart;
		public int mjrDmgStart;
		public int critStart;
		public int deathStart;

		private int spriteStart;
		private int currentSprite = 0;
		private float timeWait = 0f;

		//FIXME doing overlayCrit update based off heart monitor for time being
		public OverlayCrits overlayCrits;

		private void Start()
		{
			sprites = SpriteManager.ScreenUISprites["gen"];
		}

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += OnSceneChange;
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneChange;
		}

		void OnSceneChange(Scene prev, Scene next){
			if(next.name != "Lobby"){
				if (!startMonitoring) {
					startMonitoring = true;
					StartCoroutine(MonitorHealth());
				}
			} else {
				startMonitoring = false;
			}
		}

		IEnumerator MonitorHealth(){
			currentSprite = 0; //28 length for monitor anim
			while(startMonitoring){
				while(PlayGroup.PlayerManager.LocalPlayer == null){
					yield return new WaitForSeconds(1f);
				}
				pulseImg.sprite = sprites[spriteStart + currentSprite++];
				while (currentSprite == 28) {
					yield return new WaitForSeconds(0.05f);
					timeWait += Time.deltaTime;
					if (timeWait >= 3f) {
						timeWait = 0f;
						currentSprite = 0;
					}
				}

				yield return new WaitForSeconds(0.05f);
			}

			yield return new WaitForEndOfFrame();
		}

		public void DetermineDisplay(PlayerHealthUI pHealthUI, int maxHealth){
			if (pHealthUI == null)
				return;

			CheckHealth(maxHealth);
		}

		private void CheckHealth(int mHealth){
			if (mHealth >= 100
			    && spriteStart != fullHealthStart){
				SoundManager.Stop("Critstate");
				spriteStart = fullHealthStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.normal);
			}
			if(mHealth < 100
			   && mHealth > 80
			   && spriteStart != minorDmgStart){
				SoundManager.Stop("Critstate");
				spriteStart = minorDmgStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.injured);
			}
			if (mHealth < 80
			    && mHealth > 50
			    && spriteStart != medDmgStart) {
				SoundManager.Stop("Critstate");
				spriteStart = medDmgStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.injured);
			}
			if (mHealth < 50
			    && mHealth > 30
				&& spriteStart != mjrDmgStart) {
				SoundManager.Play("Critstate");
				spriteStart = mjrDmgStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.unconscious);
			}
			if (mHealth < 20
			    && mHealth > 0
				&& spriteStart != critStart) {
				spriteStart = critStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.crit);
			}

			if (mHealth <= 0
				&& spriteStart != deathStart) {
				SoundManager.Stop("Critstate");
				spriteStart = deathStart;
				pulseImg.sprite = sprites[spriteStart];
				overlayCrits.SetState(OverlayState.death);
			}
		}
	}
}
