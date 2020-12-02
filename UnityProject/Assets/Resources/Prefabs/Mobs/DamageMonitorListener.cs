using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DamageMonitorListener : MonoBehaviour
{
	public BodyPartType bodyPartType;

	[SerializeField] private Image bodyPartImage = default;
	[SerializeField] private Image damageMaskImage = default;

	private void Awake()
	{
		if(damageMaskImage == null)
			Logger.LogWarning($"Missing reference on {name}.DamageMonitorListener.{nameof(damageMaskImage)}", Category.UI);
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
		SetDamageColor(new Color());
		SetBodyPartColor(Color.white);
	}

	public void SetBodyPartColor(Color color)
	{
		if(bodyPartImage == null)
			return;

		bodyPartImage.color = color;
	}

	public void SetDamageColor(Color color)
	{
		if (damageMaskImage == null)
			return;
		
		damageMaskImage.color = color;
	}
}
