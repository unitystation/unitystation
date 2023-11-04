using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine;

public class ClientAlertManager : MonoBehaviour
{
	public List<AlertUIElement> RegisteredAlerts = new List<AlertUIElement>();

	public AlertUIElement PrefabAlertUIElement;

	public GameObject UIArea;

	public event System.Action<AlertUIElement> OnActionShown;
	public event System.Action<AlertUIElement> OnActionHidden;


	public void ShowingAction(AlertUIElement toShow)
	{
		OnActionShown?.Invoke(toShow);
	}

	public void HidingAction(AlertUIElement toHide)
	{
		OnActionHidden?.Invoke(toHide);
	}

	public void RegisterAlert(AlertSO alertSo)
	{
		this.gameObject.SetActive(true);
		var newAlert = Instantiate(PrefabAlertUIElement, UIArea.transform);
		newAlert.AlertSO = alertSo;
		RegisteredAlerts.Add(newAlert);
		newAlert.Initialise();

		foreach (var Alert in RegisteredAlerts)
		{
			Alert.StateChangeThisUpdate = false;
		}
	}

	public void UnRegisterAlert(AlertSO AlertSO)
	{
		AlertUIElement AlertUIElement = null;
		foreach (var alert in RegisteredAlerts)
		{
			if (alert.AlertSO == AlertSO)
			{
				AlertUIElement = alert;
				break;
			}
		}

		if (AlertUIElement == null)
		{
			Loggy.LogError($"you can find any actions associated with {AlertSO.name}");
			return;
		}

		RegisteredAlerts.Remove(AlertUIElement);
		HidingAction(AlertUIElement);
		Destroy(AlertUIElement.gameObject);

		foreach (var Alert in RegisteredAlerts)
		{
			Alert.StateChangeThisUpdate = false;
		}
	}

	public void UnRegisterAlertALL()
	{
		var Copy = RegisteredAlerts.ToList();
		foreach (var Alert in Copy)
		{
			UnRegisterAlert(Alert.AlertSO);
		}
	}
}
