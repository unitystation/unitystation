using System.Collections.Generic;
using HealthV2;
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
	public SpriteHandler hungerAlert;
	public SpriteHandler bleedingAlert;
	public UI_HeartMonitor heartMonitor;
	public List<DamageMonitorListener> bodyPartListeners = new List<DamageMonitorListener>();

	[Tooltip("0-None; 1-Light; 2-LightModerate; 3-Moderate; 4-Bad; 5-Critical; 6-Max")]
	public Color[] damageMonitorColors = new Color[7];
	public Color disabledBodyPartColor;
	public Color destroyedBodyPartColor;
	public GameObject baseBody;
	public GameObject alertsBox;

	public Button oxygenButton;

	public bool humanUI;

	void Awake()
	{
		DisableAll();
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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

	void UpdateMe()
	{
		if (PlayerManager.LocalPlayerObject == null)
		{
			return;
		}

		if (PlayerManager.LocalPlayerScript.IsNormal == false)
		{
			if (humanUI)
			{
				DisableAll();
			}

			return;
		}


		if (PlayerManager.LocalPlayerScript.IsNormal && !humanUI)
		{
			EnableAlwaysVisible();
		}

		float temperature = PlayerManager.LocalPlayerScript.playerHealth.RespiratorySystem.Temperature;

		if (temperature < 110)
		{
			SetSpecificVisibility(true, coldAlert);
		}
		else
		{
			SetSpecificVisibility(false, coldAlert);
		}

		if (PlayerManager.LocalPlayerScript.playerHealth.FireStacks > 0 || temperature > 510)
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

		float pressure = PlayerManager.LocalPlayerScript.playerHealth.RespiratorySystem.Pressure;

		if (pressure > 50 && pressure < 325)
		{
			SetSpecificVisibility(false, pressureAlert.gameObject);
		}
		else
		{
			SetSpecificVisibility(true, pressureAlert.gameObject);
			pressureAlert.SetPressureSprite(pressure);
		}

		SetSpecificVisibility(PlayerManager.LocalPlayerScript.playerHealth.RespiratorySystem.IsSuffocating, oxygenAlert);

		SetSpecificVisibility(PlayerManager.LocalPlayerScript.playerHealth.HealthStateController.HasToxins, toxinAlert);

		switch (PlayerManager.LocalPlayerScript.playerHealth.HealthStateController.HungerState)
		{

			case HungerState.Full:
				hungerAlert.gameObject.SetActive(true);
				hungerAlert.ChangeSprite(0);
				break;
			case HungerState.Normal:
				hungerAlert.gameObject.SetActive(false);
				hungerAlert.PushClear();
				break;
			case HungerState.Hungry:
				hungerAlert.gameObject.SetActive(true);
				hungerAlert.ChangeSprite(1);
				break;
			case HungerState.Malnourished:
				hungerAlert.gameObject.SetActive(true);
				hungerAlert.ChangeSprite(1);
				break;
			case HungerState.Starving:
				hungerAlert.gameObject.SetActive(true);
				hungerAlert.ChangeSprite(2);
				break;
			default:
				hungerAlert.gameObject.SetActive(false);
				hungerAlert.PushClear();
				break;
		}

		switch (PlayerManager.LocalPlayerScript.playerHealth.HealthStateController.BleedingState)
		{
			case BleedingState.None:
				bleedingAlert.gameObject.SetActive(false);
				bleedingAlert.PushClear();
				break;
			case BleedingState.VeryLow:
				bleedingAlert.gameObject.SetActive(true);
				bleedingAlert.ChangeSprite(0);
				break;
			case BleedingState.Low:
				bleedingAlert.gameObject.SetActive(true);
				bleedingAlert.ChangeSprite(1);
				break;
			case BleedingState.Medium:
				bleedingAlert.gameObject.SetActive(true);
				bleedingAlert.ChangeSprite(2);
				break;
			case BleedingState.High:
				bleedingAlert.gameObject.SetActive(true);
				bleedingAlert.ChangeSprite(3);
				break;
			case BleedingState.UhOh:
				bleedingAlert.gameObject.SetActive(true);
				bleedingAlert.ChangeSprite(4);
				break;
			default:
				bleedingAlert.gameObject.SetActive(false);
				bleedingAlert.PushClear();
				break;
		}


		// if (!PlayerManager.Equipment.HasInternalsEquipped() && oxygenButton.IsInteractable())
		// {
			// EventManager.Broadcast(EVENT.DisableInternals);
			// oxygenButton.interactable = false;
		// }
	}

	/// <summary>
	/// Update the PlayerHealth body part hud icon
	/// </summary>
	/// <param name="bodyPart"> Body part that requires updating </param>
	public void SetBodyTypeOverlay(BodyPart bodyPart)
	{
		for (int i = 0; i < bodyPartListeners.Count; i++)
		{
			if (bodyPartListeners[i].BodyPartType != bodyPart.BodyPartType)
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
						bodyPartColor = damageMonitorColors[6];
						damageColor = destroyedBodyPartColor;
						break;
					default:
						bodyPartColor = damageMonitorColors[6];
						damageColor = destroyedBodyPartColor;
						break;
				}
				if (HasAuthority(bodyPart))
				{
					bodyPartListeners[i].SetDamageColor(damageColor);
					bodyPartListeners[i].SetBodyPartColor(bodyPartColor);
				}
				else
				{
					if (bodyPart.HealthMaster != null)
					{
						bodyPart.HealthMaster.HealthStateController.ServerUpdateDoll(i, damageColor,bodyPartColor);
					}
				}
			}
		}
		bool HasAuthority(BodyPart bbodyPart)
		{
			return bbodyPart.HealthMaster.hasAuthority;
		}
	}
}