using UnityEngine;

public class ControlDisplays : MonoBehaviour
	{
		public GameObject backGround;
		public RectTransform hudBottom;
		public RectTransform hudRight;
		public GameObject jobSelectWindow;
		public GameObject selfDestructVideo;
        public RectTransform panelRight;
		public UIManager parentScript;

		/// <summary>
		///     Clears all of the UI slot items
		/// </summary>
		public void ResetUI()
		{
			foreach (UI_ItemSlot itemSlot in GetComponentsInChildren<UI_ItemSlot>())
			{
				itemSlot.Reset();
			}
		}

		public void SetScreenForLobby()
		{
			SoundManager.StopAmbient();
			SoundManager.PlayRandomTrack(); //Gimme dat slap bass
			ResetUI(); //Make sure UI is back to default for next play
			hudRight.gameObject.SetActive(false);
			hudBottom.gameObject.SetActive(false);
			backGround.SetActive(true);
			panelRight.gameObject.SetActive(false);
		}

		public void SetScreenForGame()
		{
			hudRight.gameObject.SetActive(true);
			hudBottom.gameObject.SetActive(true);
			backGround.SetActive(false);
			panelRight.gameObject.SetActive(true);


			SoundManager.StopMusic();
		}

		public void HideRightPanel()
		{
		}
	}
