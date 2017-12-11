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
            if(SceneManager.GetActiveScene().name != "Lobby"){
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

        void OnSceneChange(Scene prev, Scene next)
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

        void TryStartMonitor()
        {
            if (!startMonitoring)
            {
                spriteStart = fullHealthStart;
                startMonitoring = true;
                StartCoroutine(MonitorHealth());
            }
        }

        IEnumerator MonitorHealth()
        {
            currentSprite = 0; //28 length for monitor anim
            while (startMonitoring)
            {
                while (PlayGroup.PlayerManager.LocalPlayer == null)
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
                return;
            if (curHealth <= -1 && spriteStart == deathStart)
                return; //Ensure that messages are not spammed when there is no more health to go

            CheckHealth(curHealth);
        }

        private void CheckHealth(int cHealth)
        {
            //PlayGroup.PlayerManager.PlayerScript.playerNetworkActions.CmdSendAlertMessage("mHealth: " + cHealth, true);
            //Debug.Log("PlayerHealth: " + PlayGroup.PlayerManager.PlayerScript.playerHealth.Health);
            if (cHealth >= 100
                && spriteStart != fullHealthStart)
            {
                SoundManager.Stop("Critstate");
                spriteStart = fullHealthStart;
                pulseImg.sprite = sprites[spriteStart];
                overlayCrits.SetState(OverlayState.normal);
            }
            if (cHealth < 100
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
                overlayCrits.SetState(OverlayState.crit);

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
