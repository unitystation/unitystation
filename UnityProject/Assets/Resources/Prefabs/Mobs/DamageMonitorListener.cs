using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DamageMonitorListener : MonoBehaviour
{
	public BodyPartType bodyPartType;
	[HideInInspector]
	public Image image;
	private Sprite initSprite;

	private void Awake()
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