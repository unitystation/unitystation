using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
	public UI_HeartMonitor heartMonitor;
	public OverlayCrits overlayCrits;
	private UI_OxygenAlert oxygenAlert;
	private bool monitorBreathing = false;
	private Button oxygenButton;

	List<DamageMonitorListener> bodyPartListeners = new List<DamageMonitorListener>();

	void Awake()
	{
		bodyPartListeners = new List<DamageMonitorListener>(UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>(true));
		oxygenAlert = GetComponentInChildren<UI_OxygenAlert>(true);
		oxygenAlert.gameObject.SetActive(false);
		oxygenButton = GetComponentInChildren<OxygenButton>(true).gameObject.GetComponent<Button>();
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		UpdateManager.Instance.Add(UpdateMe);
		if (SceneManager.GetActiveScene().name != "Lobby")
		{
			monitorBreathing = true;
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
	{
		if (monitorBreathing && PlayerManager.LocalPlayer != null)
		{
			if (PlayerManager.LocalPlayerScript.IsGhost || PlayerManager.LocalPlayerScript.playerHealth.IsDead)
			{
				if (oxygenAlert.gameObject.activeInHierarchy)
				{
					oxygenAlert.gameObject.SetActive(false);
				}
				return;
			}

			if (PlayerManager.LocalPlayerScript.playerHealth.IsRespiratoryArrest && !oxygenAlert.gameObject.activeInHierarchy)
			{
				oxygenAlert.gameObject.SetActive(true);
			}

			if (!PlayerManager.LocalPlayerScript.playerHealth.IsRespiratoryArrest && oxygenAlert.gameObject.activeInHierarchy)
			{
				oxygenAlert.gameObject.SetActive(false);
			}
		}

		if (PlayerManager.LocalPlayer != null)
		{
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
	}

	private void OnSceneChange(Scene prev, Scene next)
	{
		if (next.name != "Lobby")
		{
			monitorBreathing = true;
		}
		else
		{
			monitorBreathing = false;
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