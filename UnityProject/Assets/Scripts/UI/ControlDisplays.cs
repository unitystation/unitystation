﻿using UnityEngine;

namespace UI
{
    public class ControlDisplays : MonoBehaviour
    {
        public GameObject backGround;
        public RectTransform hudBottom;

        public RectTransform hudRight;
        public GameObject jobSelectWindow;
        public GameObject logInWindow;

        public RectTransform panelRight;
        public UIManager parentScript;

        /// <summary>
        ///     Clears all of the UI slot items
        /// </summary>
        public void ResetUI()
        {
            foreach (var itemSlot in GetComponentsInChildren<UI_ItemSlot>())
            {
                itemSlot.Reset();
            }
        }

        public void SetScreenForLobby()
        {
            SoundManager.StopAmbient();
            if (Time.time > 10f)
            {
                SoundManager.PlayRandomTrack();
            }
            else
            {
                //Start the patchmanager
                var patchManager = Resources.Load("PatchManager") as GameObject;
                if (patchManager != null)
                {
                    Instantiate(patchManager);
                }
            }
            ResetUI(); //Make sure UI is back to default for next play
            hudRight.gameObject.SetActive(false);
            hudBottom.gameObject.SetActive(false);
            backGround.SetActive(true);
            logInWindow.SetActive(true);
        }

        public void SetScreenForGame()
        {
            hudRight.gameObject.SetActive(true);
            hudBottom.gameObject.SetActive(true);
            backGround.SetActive(false);


            SoundManager.StopMusic();
            //			//TODO random ambient
            //			int rA = Random.Range(0,2);
            //			SoundManager.PlayVarAmbient(rA);
        }

        public void HideRightPanel()
        {
        }
    }
}