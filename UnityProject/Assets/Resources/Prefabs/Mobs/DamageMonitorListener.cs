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
		SceneManager.activeSceneChanged += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnLevelFinishedLoading;
	}

	//Reset healthHUD
	private void OnLevelFinishedLoading(Scene oldScene, Scene newScene)
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