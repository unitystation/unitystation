using System.Collections;
using System.Collections.Generic;
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
		var bloodTotal = Mathf.Round(health.CirculatorySystem.UsedBloodPool[health.CirculatorySystem.Blood]
			+ health.CirculatorySystem.ReadyBloodPool[health.CirculatorySystem.Blood]) * 1000;
		var bloodPercent = Mathf.Round(bloodTotal / health.CirculatorySystem.BloodInfo.BLOOD_NORMAL * 100);



		var availOxy = health.CirculatorySystem.ReadyBloodPool[GAS2ReagentSingleton.Instance.Oxygen];

		string ToShow = ("----------------------------------------\n" +
						targetName + " is " + health.ConsciousState.ToString() + "\n" +
						"Overall status: " + totalPercent + "% health\n" +
						"Blood level = " + bloodPercent + "%, " + bloodTotal + "cc\n" +
						"Oxy Level = " + availOxy + "cc\n");
		string StringBuffer = "";
		float[] fullDamage = new float[7];

		foreach (var BodyPart in health.ImplantList)
		{
			if (AdvancedHealthScanner == false && BodyPart.DamageContributesToOverallHealth == false) continue;
			if (BodyPart.TotalDamage == 0) continue;

			for (int i = 0; i < BodyPart.Damages.Length; i++)
			{
				fullDamage[i] += BodyPart.Damages[i];
			}

			StringBuffer += BodyPart.gameObject.ExpensiveName() + "\t  ";
			StringBuffer += Mathf.Round(BodyPart.Brute) + "\t  ";
			StringBuffer += Mathf.Round(BodyPart.Burn) + "\t  ";
			StringBuffer += Mathf.Round(BodyPart.Toxin) + "\t    ";
			StringBuffer += Mathf.Round(BodyPart.Oxy) + "\n";
		}

		ToShow += "General status:\nDamage:\tBrute\tBurn\tToxin\tSuffocation\n" +
					$"Overall:\t  {fullDamage[(int)DamageType.Brute]}\t  {fullDamage[(int)DamageType.Burn]}\t  " +
					$"{fullDamage[(int)DamageType.Tox]}\t    {fullDamage[(int)DamageType.Oxy]}\n" + StringBuffer;

		Chat.AddExamineMsgFromServer(interaction.Performer, ToShow);
	}
}