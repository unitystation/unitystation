using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HealthV2;
using UnityEngine;

/// <summary>
/// Main health scanner interaction. Applying it to a living thing sends a request to the server to
/// tell us their health info.
/// </summary>
public class HealthScanner : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public bool AdvancedHealthScanner = false;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var performerName = interaction.Performer.ExpensiveName();
		var targetName = interaction.TargetObject.ExpensiveName();
		Chat.AddActionMsgToChat(interaction.Performer, $"You analyze {targetName}'s vitals.",
					$"{performerName} analyzes {targetName}'s vitals.");

		var health = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		var totalPercent = Mathf.Round(100 * health.OverallHealth / health.MaxHealth);
		var bloodTotal = Mathf.Round(health.GetTotalBlood()*1000);
		var bloodPercent = Mathf.Round(bloodTotal / health.CirculatorySystem.BloodInfo.BLOOD_NORMAL * 100);
		//var availOxy = health.CirculatorySystem.ReadyBloodPool[GAS2ReagentSingleton.Instance.Oxygen];

		string ToShow = ("----------------------------------------\n" +
						targetName + " is " + health.ConsciousState.ToString() + "\n" +
						"<b>Overall status: " + totalPercent + "% healthy</b>\n" +
						"Blood level = " + bloodPercent + "%, " + bloodTotal + "cc\n");
		string StringBuffer = "";
		float[] fullDamage = new float[7];
		TextInfo textInfo = new CultureInfo("en-US",false).TextInfo;

		foreach (var BodyPart in health.ImplantList)
		{
			if (AdvancedHealthScanner == false && BodyPart.DamageContributesToOverallHealth == false) continue;
			if (BodyPart.TotalDamage == 0) continue;

			for (int i = 0; i < BodyPart.Damages.Length; i++)
			{
				fullDamage[i] += BodyPart.Damages[i];
			}

			string partName = BodyPart.gameObject.ExpensiveName();

			// Not the best way to do this, need a list of races
			if (partName.StartsWith("human ") || partName.StartsWith("lizard ")  || partName.StartsWith("moth ") || partName.StartsWith("cat "))
			{
				int i = partName.IndexOf(" ")+1;
				partName = partName.Substring(i);
			}
			partName = textInfo.ToTitleCase(partName);
			if(partName.Length < 6)
				partName += "\t";
			if(partName.Length < 9)
				partName += "\t";
			
			StringBuffer += $"{partName}\t\t  ";
			StringBuffer += $"<color=brown>{Mathf.Round(BodyPart.Brute)}</color>\t\t   ";
			StringBuffer += $"<color=orange>{Mathf.Round(BodyPart.Burn)}</color>\t\t   ";
			StringBuffer += $"<color=lime>{Mathf.Round(BodyPart.Toxin)}</color>\t\t  ";
			StringBuffer += $"<color=cyan>{Mathf.Round(BodyPart.Oxy)}</color>\n";
		}

		ToShow += "General Status:\n<b>Damage:\t\t<color=brown>Brute</color>\t<color=orange>Burn</color>\t<color=lime>Toxin</color>\t<color=cyan>Oxy</color>\n" +
					$"Overall:\t\t\t   <color=brown>{Mathf.Round(fullDamage[(int)DamageType.Brute])}</color>\t\t" +
					$"    <color=orange>{Mathf.Round(fullDamage[(int)DamageType.Burn])}</color>\t\t" +
					$"    <color=lime>{Mathf.Round(fullDamage[(int)DamageType.Tox])}</color>\t\t" +
					$"  <color=cyan>{Mathf.Round(fullDamage[(int)DamageType.Oxy])}</color></b>\n" + StringBuffer +
					"\n----------------------------------------";

		Chat.AddExamineMsgFromServer(interaction.Performer, ToShow);
	}
}