using System.Text;
using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using Util;

namespace US13.Items.Medical
{
	/// <summary>
	/// Main interaction. Applying it to a reagentcontainer sends a request to the server to
	/// tell us its info.
	/// </summary>
	public class ReagentScanner : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private bool isGoggles = false;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			// can only be applied to reagents or items with forensics script
			return Validations.HasComponent<ReagentContainer>(interaction.TargetObject);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(isGoggles == false)
			{
				DoScan(interaction.Performer, interaction.TargetObject);
			}

		}
		public void DoScan(GameObject Performer, GameObject TargetObject)
		{

			var performerName = Performer.ExpensiveName();
			var targetName = TargetObject.ExpensiveName();

			Chat.AddActionMsgToChat(Performer,
					$"You scan the {targetName}.",
					$"{performerName} scans the {targetName}.");

			var reagents = TargetObject.GetComponent<ReagentContainer>().CurrentReagentMix;
			StringBuilder scanMessage;
			if (reagents.Total > 0f)
			{
				scanMessage = new StringBuilder(
				"----------------------------------------\n" +
				$"{targetName} contains {reagents.Total} units of {TextUtils.ColorToString(reagents.MixColor)} {ChemistryUtils.GetMixStateDescription(reagents)}\n" +
				$"<b>Its Chemical make up is: \n");

				foreach (var reagent in reagents.reagents)
				{
					scanMessage.Append("<mspace=0.6em>");
					scanMessage.AppendLine(
							$"<color=#{ColorUtility.ToHtmlStringRGB(reagent.Key.color)}><b>{reagent.Key}</b> - {reagent.Value.ToString("F3")}u</color>"
					);
					scanMessage.Append("</mspace>");
				}
				scanMessage.Append("----------------------------------------");
			}
			else
			{
				scanMessage = new StringBuilder("No reagents found");
			}


			Chat.AddExamineMsgFromServer(Performer, $"</i>{scanMessage}<i>");
		}
	}
}