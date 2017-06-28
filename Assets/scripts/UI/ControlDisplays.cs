using UnityEngine;
using System.Collections;

namespace UI
{
	public class ControlDisplays: MonoBehaviour
	{
		public UIManager parentScript;
		public GameObject logInWindow;
		public GameObject backGround;
		public GameObject[] UIObjs;
        public GameObject jobSelectWindow;

		public void ResetUI()
		{
			foreach (var itemSlot in GetComponentsInChildren<UI_ItemSlot>()) {
				itemSlot.Reset();
			}
		}

		public void SetScreenForLobby()
		{
			SoundManager.StopAmbient();
			SoundManager.PlayRandomTrack();
			ResetUI(); //Make sure UI is back to default for next play
			foreach (GameObject obj in UIObjs) {
				obj.SetActive(false);
			}
			backGround.SetActive(true);
			logInWindow.SetActive(true);
		}

		public void SetScreenForGame()
		{
			foreach (GameObject obj in UIObjs) {
				obj.SetActive(true);
			}
			backGround.SetActive(false);


			SoundManager.StopMusic();
			//TODO random ambient
			SoundManager.PlayVarAmbient(0);
		}
			
	}
}
