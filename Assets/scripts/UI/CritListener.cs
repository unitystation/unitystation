using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI
{
    public class CritListener : MonoBehaviour
    {
        public Sprite critSprite;
		public Sprite fullHealthSprite;

		void OnEnable()
		{
			SceneManager.sceneLoaded += OnLevelFinishedLoading;
		}

		void OnDisable()
		{
			SceneManager.sceneLoaded -= OnLevelFinishedLoading;
		}
        public void ShowCrit()
        {
            Debug.Log("Setting Crit");
            SoundManager.Play("Critstate");
            GetComponent<Image>().sprite = critSprite;
        }

		void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode){
			GetComponent<Image>().sprite = fullHealthSprite;
			SoundManager.Stop("Critstate");
		}
    }
}