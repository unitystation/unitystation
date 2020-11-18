using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
	public GameObject toxinAlert;
	public GameObject heatAlert;
	public GameObject coldAlert;
	public UI_PressureAlert pressureAlert;
	public GameObject oxygenAlert;
	public UI_TemperatureAlert temperatureAlert;
	public GameObject hungerAlert;
	public UI_HeartMonitor heartMonitor;
	public List<DamageMonitorListener> bodyPartListeners = new List<DamageMonitorListener>();

	[Tooltip("0-None; 1-Light; 2-LightModerate; 3-Moderate; 4-Bad; 5-Critical; 6-Max")]
	public Color[] damageMonitorColors = new Color[7];
	public Color disabledBodyPartColor;
	public Color destroyedBodyPartColor;
	public GameObject baseBody;
	public GameObject alertsBox;

	public bool humanUI;

	void Awake()
	{
		DisableAll();
	}

	private void DisableAll()
	{
		Transform[] childrenList = GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < childrenList.Length; i++)
		{
			var children = childrenList[i].gameObject;
			if (children == gameObject)
			{
				continue;
			}
			children.SetActive(false);
		}
		humanUI = false;
	}

	private void EnableAlwaysVisible()
	{
		heartMonitor.gameObject.SetActive(true);
		for (int i = 0; i < bodyPartListeners.Count; i++)
		{
			bodyPartListeners[i].gameObject.SetActive(true);
		}
		baseBody.SetActive(true);
		alertsBox.SetActive(true);
		humanUI = true;
	}

	void SetSpecificVisibility(bool value, GameObject UIelement)
	{
		if (UIelement.activeInHierarchy != value)
		{
			UIelement.SetActive(value);
		}
	}

	void Update()
	{
		if (PlayerManager.LocalPlayer == null)
		{
			return;
		}

		if (PlayerManager.LocalPlayerScript.IsGhost)
		{
			if (humanUI)
			{
				DisableAll();
			}
			return;
		}


		if (!PlayerManager.LocalPlayerScript.IsGhost && !humanUI)
		{
			EnableAlwaysVisible();
		}

		float temperature = PlayerManager.LocalPlayerScript.playerHealth.respiratorySystem.temperature;
		float pressure = PlayerManager.LocalPlayerScript.playerHealth.respiratorySystem.pressure;

		if (temperature < 110)
		{
			SetSpecificVisibility(true, coldAlert);
		}
		else
		{
			SetSpecificVisibility(false, coldAlert);
		}

		if (temperature > 510)
		{
			SetSpecificVisibility(true, heatAlert);
		}
		else
		{
			SetSpecificVisibility(false, heatAlert);
		}


		if (temperature > 260 && temperature < 360)
		{
			SetSpecificVisibility(false, temperatureAlert.gameObject);
		}
		else
		{
			SetSpecificVisibility(true, temperatureAlert.gameObject);
			temperatureAlert.SetTemperatureSprite(temperature);
		}

		if (pressure > 50 && pressure < 325)
		{
			SetSpecificVisibility(false, pressureAlert.gameObject);
		}
		else
		{
			SetSpecificVisibility(true, pressureAlert.gameObject);
			pressureAlert.SetPressureSprite(pressure);
		}

		SetSpecificVisibility(PlayerManager.LocalPlayerScript.playerHealth.respiratorySystem.IsSuffocating, oxygenAlert);

		SetSpecificVisibility(false, toxinAlert);
		SetSpecificVisibility(PlayerManager.LocalPlayerScript.playerHealth.Metabolism.IsHungry, hungerAlert);
	}

	/// <summary>
	/// Update the PlayerHealth body part hud icon
	/// </summary>
	/// <param name="bodyPart"> Body part that requires updating </param>
	public void SetBodyTypeOverlay(BodyPartBehaviour bodyPart)
	{
		for (int i = 0; i < bodyPartListeners.Count; i++)
		{
			if (bodyPartListeners[i].bodyPartType != bodyPart.Type)
			{
				continue;
			}
			if (bodyPartListeners[i] != null)
			{
				Color damageColor = Color.clear;
				Color bodyPartColor = Color.white;
				switch (bodyPart.Severity)
				{
					case DamageSeverity.None:
						bodyPartColor = damageMonitorColors[0];
						break;
					case DamageSeverity.Light:
						bodyPartColor = damageMonitorColors[1];
						break;
					case DamageSeverity.LightModerate:
						bodyPartColor = damageMonitorColors[2];
						break;
					case DamageSeverity.Moderate:
						bodyPartColor = damageMonitorColors[3];
						break;
					case DamageSeverity.Bad:
						bodyPartColor = damageMonitorColors[4];
						break;
					case DamageSeverity.Critical:
						bodyPartColor = damageMonitorColors[5];
						damageColor = disabledBodyPartColor;
						break;
					case DamageSeverity.Max:
					default:
						bodyPartColor = damageMonitorColors[6];
						damageColor = destroyedBodyPartColor;
						break;
				}

				bodyPartListeners[i].SetDamageColor(damageColor);
				bodyPartListeners[i].SetBodyPartColor(bodyPartColor);
			}
		}
	}
}