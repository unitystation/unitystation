using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DamageMonitorListener : MonoBehaviour
{
	public BodyPartType bodyPartType;
	private Image image;
	private Sprite initSprite;

	private void Start()
	{
		image = GetComponent<Image>();
		initSprite = image.sprite;
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	public void UpdateDamageSeverity(int severity)
	{
	}

	//Reset healthHUD
	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		Reset();
	}

	public void Reset()
	{
		if (image != null)
		{
			image.sprite = initSprite;
		}
	}
}