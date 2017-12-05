using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    public class ControlDisplays : MonoBehaviour
    {
        public UIManager parentScript;
        public GameObject logInWindow;
        public GameObject backGround;
        public GameObject jobSelectWindow;

        public RectTransform hudRight;
        public RectTransform hudBottom;

        public RectTransform panelRight;

        /// <summary>
        /// Clears all of the UI slot items
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
                GameObject patchManager = Resources.Load("PatchManager") as GameObject;
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
