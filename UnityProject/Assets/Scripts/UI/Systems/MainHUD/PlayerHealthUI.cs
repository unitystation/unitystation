using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
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

		if (PlayerManager.LocalPlayerScript == null || PlayerManager.LocalPlayerScript.IsNormal == false)
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