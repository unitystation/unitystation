using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using HealthV2;
using UnityEngine;

namespace Items.Medical
{
	/// <summary>
	/// Main health scanner interaction. Applying it to a living thing sends a request to the server to
	/// tell us their health info.
	/// </summary>
	public class HealthScanner : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public bool AdvancedHealthScanner = false;

		private string bruteColor;
		private string burnColor;
		private string toxinColor;
		private string oxylossColor;

		private TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

		private void Awake()
		{
			bruteColor = ColorUtility.ToHtmlStringRGB(Color.red);
			burnColor = ColorUtility.ToHtmlStringRGB(Color.yellow);
			toxinColor = ColorUtility.ToHtmlStringRGB(Color.green);
			oxylossColor = ColorUtility.ToHtmlStringRGB(new Color(0.50f, 0.50f, 1));
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			// can only be applied to LHB
			return Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var performerName = interaction.Performer.ExpensiveName();
			var targetName = interaction.TargetObject.ExpensiveName();
			Chat.AddActionMsgToChat(interaction.Performer,
					$"You analyze {targetName}'s vitals.",
					$"{performerName} analyzes {targetName}'s vitals.");

			var health = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			var totalPercent = Mathf.Round(100 * health.OverallHealth / health.MaxHealth);
			var bloodTotal = Mathf.Round(health.GetTotalBlood());
			var bloodPercent = Mathf.Round(bloodTotal / health.CirculatorySystem.BloodInfo.BLOOD_NORMAL * 100);
			float[] fullDamage = new float[7];

			StringBuilder scanMessage = new StringBuilder(
					"----------------------------------------\n" +
					$"{targetName} is {health.ConsciousState}\n" +
					$"<b>Overall status: {totalPercent} % healthy</b>\n" +
					$"Blood level: {bloodTotal}cc, {bloodPercent} %\n");
			StringBuilder partMessages = new StringBuilder();
			foreach (var bodypart in health.BodyPartList)
			{
				if (AdvancedHealthScanner == false && bodypart.DamageContributesToOverallHealth == false) continue;
				if (bodypart.TotalDamage == 0) continue;

				for (int i = 0; i < bodypart.Damages.Length; i++)
				{
					fullDamage[i] += bodypart.Damages[i];
				}
				partMessages.AppendLine(GetBodypartMessage(bodypart));
			}

			if (health.brain)
			{
				fullDamage[(int) DamageType.Oxy] = health.brain.RelatedPart.Oxy;
			}

			if ((int)totalPercent != 100)
			{
				scanMessage.Append("<mspace=0.6em>");
				scanMessage.AppendLine(
						$"<color=#{bruteColor}><b>{"Brute", -8}</color><color=#{burnColor}>{"Burn", -8}</color>" +
						$"<color=#{toxinColor}>{"Toxin", -8}</color><color=#{oxylossColor}>Oxy</color></b>\n" +
						$"<color=#{bruteColor}>{Mathf.Round(fullDamage[(int)DamageType.Brute]), 16}</color>" +
						$"<color=#{burnColor}>{Mathf.Round(fullDamage[(int)DamageType.Burn]), 4}</color>" +
						$"<color=#{toxinColor}>{Mathf.Round(fullDamage[(int)DamageType.Tox]), 4}</color>" +
						$"<color=#{oxylossColor}>{Mathf.Round(fullDamage[(int)DamageType.Oxy]), 4}</color>"
				);
				scanMessage.Append(partMessages);
				scanMessage.Append("</mspace>");
			}

			if (interaction.IsAltClick && AdvancedHealthScanner)
			{
				foreach(BodyPart part in health.BodyPartList)
				{
					if(part.BodyPartType == interaction.TargetBodyPart)
					{
						scanMessage.AppendLine(part.GetFullBodyPartDamageDescReport());
					}
				}
			}

			scanMessage.Append("----------------------------------------");

			Chat.AddExamineMsgFromServer(interaction.Performer, $"</i>{scanMessage}<i>");
		}

		private string GetBodypartMessage(BodyPart bodypart)
		{
			string partName = bodypart.gameObject.ExpensiveName();

			// Not the best way to do this, need a list of races
			if (partName.StartsWith("human ") || partName.StartsWith("lizard ") || partName.StartsWith("moth ") || partName.StartsWith("catperson "))
			{
				partName = partName.Substring(partName.IndexOf(" ") + 1);
			}

			return $"{textInfo.ToTitleCase(partName),-12}" +
			       $"<color=#{bruteColor}>{Mathf.Round(bodypart.Brute),4}</color>" +
			       $"<color=#{burnColor}>{Mathf.Round(bodypart.Burn),4}</color>" +
			       $"<color=#{toxinColor}>{Mathf.Round(bodypart.Toxin),4}</color>";
		}
	}
}
