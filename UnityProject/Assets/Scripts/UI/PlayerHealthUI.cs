using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
	public UI_HeartMonitor heartMonitor;
	public OverlayCrits overlayCrits;
	private UI_OxygenAlert oxygenAlert;
	public UI_PressureAlert.PressureChecker pressureStatusCache = UI_PressureAlert.PressureChecker.noAlert;
	public UI_TempAlert.TempChecker tempStatusCache = UI_TempAlert.TempChecker.noAlert;
	private UI_TempAlert tempAlert;
	private UI_PressureAlert pressureAlert;
	private bool monitorBreathing = false;
	private bool monitorTemp = false;
	private bool monitorPressure = false;
	private bool hasOxygen = false;

	private Button oxygenButton;
	List<DamageMonitorListener> bodyPartListeners = new List<DamageMonitorListener>();

	void Awake()
	{
		bodyPartListeners = new List<DamageMonitorListener>(UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>(true));
		oxygenAlert = GetComponentInChildren<UI_OxygenAlert>(true);
		tempAlert = GetComponentInChildren<UI_TempAlert>(true);
		pressureAlert = GetComponentInChildren<UI_PressureAlert>(true);
		oxygenAlert.gameObject.SetActive(false);
		tempAlert.gameObject.SetActive(false);
		pressureAlert.gameObject.SetActive(false);
		oxygenButton = GetComponentInChildren<OxygenButton>(true).gameObject.GetComponent<Button>();
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		UpdateManager.Instance.Add(UpdateMe);
		if (SceneManager.GetActiveScene().name != "Lobby")
		{
			monitorBreathing = true;
			monitorTemp = true;
			monitorPressure = true;
		}
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	void UpdateMe()
	{	//Doesn't update if player doesn't exist
		if (PlayerManager.LocalPlayer == null)
		{
			return;
		}
		if (PlayerManager.LocalPlayerScript.IsGhost || PlayerManager.LocalPlayerScript.playerHealth.IsDead)
			{
				if (oxygenAlert.gameObject.activeInHierarchy)
				{
					oxygenAlert.gameObject.SetActive(false);
				}
				if (tempAlert.gameObject.activeInHierarchy)
				{
					tempAlert.gameObject.SetActive(false);
				}
				if (pressureAlert.gameObject.activeInHierarchy)
				{
					pressureAlert.gameObject.SetActive(false);
				}
				return;
			}

		if (monitorBreathing)
		{ 
			
			
			hasOxygen = !PlayerManager.LocalPlayerScript.playerHealth.IsRespiratoryArrest && !PlayerManager.LocalPlayerScript.playerHealth.respiratorySystem.IsSuffocating;
			if (!hasOxygen && !oxygenAlert.gameObject.activeInHierarchy)
			{
				oxygenAlert.gameObject.SetActive(true);
			}

			if (hasOxygen && oxygenAlert.gameObject.activeInHierarchy)
			{
				oxygenAlert.gameObject.SetActive(false);
			}
		}
		// Handles temperature alert UI element
		if (monitorTemp && tempStatusCache != PlayerManager.LocalPlayerScript.playerHealth.TempStatus)
		{
			tempStatusCache = PlayerManager.LocalPlayerScript.playerHealth.TempStatus;
			if ((tempStatusCache == UI_TempAlert.TempChecker.tooHigh | tempStatusCache == UI_TempAlert.TempChecker.tooLow) && !tempAlert.gameObject.activeInHierarchy)
			{	
				Logger.LogError("Enabled Temp Guage");
				tempAlert.gameObject.SetActive(true);
			}
			if ((tempStatusCache == UI_TempAlert.TempChecker.noAlert) && tempAlert.gameObject.activeInHierarchy)
			{
				Logger.LogError("Disabled Temp Guage");
				tempAlert.gameObject.SetActive(false);
			}

		}
		// Handles pressure alert UI element
		if (monitorPressure && (pressureStatusCache != PlayerManager.LocalPlayerScript.playerHealth.PressureStatus))
		{
			pressureStatusCache = PlayerManager.LocalPlayerScript.playerHealth.PressureStatus;
			Logger.LogWarning(pressureStatusCache.ToString());
			if ((pressureStatusCache == UI_PressureAlert.PressureChecker.tooHigh | pressureStatusCache == UI_PressureAlert.PressureChecker.tooLow) && !pressureAlert.gameObject.activeInHierarchy)
			{
				Logger.LogError("Enabled Pressure Gauge");
				pressureAlert.gameObject.SetActive(true);
			}
			if ((pressureStatusCache == UI_PressureAlert.PressureChecker.noAlert) && pressureAlert.gameObject.activeInHierarchy)
			{
				Logger.LogError("Disabled Pressure Gauge");
				pressureAlert.gameObject.SetActive(false);
			}

		}

		if (PlayerManager.Equipment.HasInternalsEquipped() && !oxygenButton.IsInteractable())
		{
			oxygenButton.interactable = true;
		}

		if (!PlayerManager.Equipment.HasInternalsEquipped() && oxygenButton.IsInteractable())
		{
			EventManager.Broadcast(EVENT.DisableInternals);
			oxygenButton.interactable = false;
		}
	}

	private void OnSceneChange(Scene prev, Scene next)
	{
		if (next.name != "Lobby")
		{
			monitorBreathing = true;
			monitorTemp = true;
			monitorPressure = true;
		}
		else
		{
			monitorBreathing = false;
			monitorTemp = false;
			monitorPressure = false;
		}
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
			Sprite sprite;
			switch (bodyPart.Severity)
			{
				case DamageSeverity.None:
					sprite = bodyPart.BlueDamageMonitorIcon;
					break;
				case DamageSeverity.Light:
					sprite = bodyPart.GreenDamageMonitorIcon;
					break;
				case DamageSeverity.LightModerate:
					sprite = bodyPart.YellowDamageMonitorIcon;
					break;
				case DamageSeverity.Moderate:
					sprite = bodyPart.OrangeDamageMonitorIcon;
					break;
				case DamageSeverity.Bad:
					sprite = bodyPart.DarkOrangeDamageMonitorIcon;
					break;
				case DamageSeverity.Critical:
					sprite = bodyPart.RedDamageMonitorIcon;
					break;
				case DamageSeverity.Max:
				default:
					sprite = bodyPart.GrayDamageMonitorIcon;
					break;
			}
			if (sprite != null)
			{
				bodyPartListeners[i].image.sprite = sprite;
			}
		}
	}
}