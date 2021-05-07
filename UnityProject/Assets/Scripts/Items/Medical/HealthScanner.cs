using System.Collections;
using System.Collections.Generic;
using System.Text;
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
		var bloodTotal = Mathf.Round(health.GetTotalBlood() * 1000);
		var bloodPercent = Mathf.Round(bloodTotal / health.CirculatorySystem.BloodInfo.BLOOD_NORMAL * 100);
		//var availOxy = health.CirculatorySystem.ReadyBloodPool[GAS2ReagentSingleton.Instance.Oxygen];
		float[] fullDamage = new float[7];
		TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
		StringBuilder toShow = new StringBuilder();
		StringBuilder bodyParts = new StringBuilder();
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
			if (partName.StartsWith("human ") || partName.StartsWith("lizard ") || partName.StartsWith("moth ") || partName.StartsWith("cat "))
			{
				partName = partName.Substring(partName.IndexOf(" ") + 1);
			}
			bodyParts.Append($"{textInfo.ToTitleCase(partName)}:");

			bodyParts.Append(	$"\t<color=red>{Mathf.Round(BodyPart.Brute)}</color>" +
			                    $"\t<color=orange>{Mathf.Round(BodyPart.Burn)}</color>" +
			                    $"\t<color=green>{Mathf.Round(BodyPart.Toxin)}</color>" +
			                    $"\t<color=blue>{Mathf.Round(BodyPart.Oxy)}</color>\n");
		}

		toShow.Append("----------------------------------------\n" +
		                 targetName + " is " + health.ConsciousState + "\n" +
		                 "<b>Overall status: " + totalPercent + "% healthy</b>\n" +
		                 "Blood level = " + bloodPercent + "%, " + bloodTotal + "cc\n");

		if ((int)totalPercent != 100)
		{
			toShow.Append($"<color=red><b>Brute</color>\t<color=orange>Burn</color>\t" +
			              $"<color=green>Toxin</color>\t<color=blue>Oxy</color></b>\n" +
			              $"<color=red>{Mathf.Round(fullDamage[(int)DamageType.Brute])}</color>\t\t" +
			              $"<color=orange>{Mathf.Round(fullDamage[(int)DamageType.Burn])}</color>\t\t" +
			              $"<color=green>{Mathf.Round(fullDamage[(int)DamageType.Tox])}</color>\t\t" +
			              $"<color=blue>{Mathf.Round(fullDamage[(int)DamageType.Oxy])}</color>\n"
			);
		}

		toShow.Append(bodyParts);
		toShow.Append("----------------------------------------");

		Chat.AddExamineMsgFromServer(interaction.Performer, toShow.ToString());
	}
}