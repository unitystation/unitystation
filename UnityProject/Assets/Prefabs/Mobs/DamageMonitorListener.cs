using Logs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DamageMonitorListener : MonoBehaviour
{
	public BodyPartType BodyPartType;

	[SerializeField] private Image bodyPartImage = default;
	[SerializeField] private Image damageMaskImage = default;

	private void Awake()
	{
		if(damageMaskImage == null)
			Loggy.LogWarning($"Missing reference on {name}.DamageMonitorListener.{nameof(damageMaskImage)}", Category.UI);
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
