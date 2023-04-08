using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

public class BodyPartAlerts : BodyPartFunctionality
{
	private List<AlertSO> alerts = new List<AlertSO>();


	public void AddAlert(AlertSO alert)
	{
		alerts.Add(alert);
		if (RelatedPart.HealthMaster != null)
		{
			RelatedPart.HealthMaster.GetComponent<BodyAlertManager>().RegisterAlert(alert);
		}

	}

	public void RemoveAlert(AlertSO alert)
	{
		alerts.Remove(alert);
		if (RelatedPart.HealthMaster != null)
		{
			RelatedPart.HealthMaster.GetComponent<BodyAlertManager>().UnRegisterAlert(alert);
		}
	}


	public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
	{
		var AlertManager = livingHealth.GetComponent<BodyAlertManager>();
		foreach (var alert in alerts)
		{
			AlertManager.UnRegisterAlert(alert);
		}
	}

	public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
	{
		var AlertManager = livingHealth.GetComponent<BodyAlertManager>();
		foreach (var alert in alerts)
		{
			AlertManager.RegisterAlert(alert);
		}
	} //Warning only add body parts do not remove body parts in this
}
