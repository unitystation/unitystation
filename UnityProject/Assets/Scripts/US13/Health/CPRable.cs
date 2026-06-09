using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours.Meta;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Health
{
	/// <summary>
	/// Allows an object to be CPRed by a player.
	/// </summary>
	public class CPRable : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		const float CPR_TIME = 5;

		private static readonly StandardProgressActionConfig CPRProgressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.CPR);

		private string performerName;
		private string targetName;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.Intent != Intent.Help) return false;
			if (interaction.HandObject != null) return false;
			if (interaction.TargetObject == interaction.Performer) return false;

			if (interaction.TargetObject.TryGetComponent(out LivingHealthMasterBase livingHealth))
			{
				if (livingHealth.ConsciousState == ConsciousState.CONSCIOUS) return false;
			}

			var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();
			if (performerRegisterPlayer.IsLayingDown) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			performerName = interaction.Performer.ExpensiveName();
			targetName = interaction.TargetObject.ExpensiveName();

			var cardiacArrestPlayerRegister = interaction.TargetObject.GetComponent<RegisterPlayer>();

			void ProgressComplete()
			{
				ServerDoCPR(interaction.Performer, interaction.TargetObject, interaction.TargetBodyPart);
			}

			var cpr = StandardProgressAction.Create(CPRProgressConfig, ProgressComplete)
				.ServerStartProgress(cardiacArrestPlayerRegister, CPR_TIME, interaction.Performer);
			if (cpr != null)
			{
				Chat.AddActionMsgToChat(
					interaction.Performer,
					$"You begin performing CPR on {targetName}'s " + interaction.TargetBodyPart,
					$"{performerName} is trying to perform CPR on {targetName}'s " + interaction.TargetBodyPart);
			}
		}

		private void ServerDoCPR(GameObject performer, GameObject target, BodyPartType TargetBodyPart)
		{
			var health = target.GetComponent<LivingHealthMasterBase>();
			Vector3Int position = health.ObjectBehaviour.registerTile.WorldPosition;
			MetaDataNode node = MatrixManager.GetMetaDataAt(position);

			bool hasLung = false;
			bool hasHeart = false;
			foreach (var bodyPart in health.BodyPartList)
			{
				if (bodyPart.BodyPartType == TargetBodyPart)
				{
					foreach (var organ in bodyPart.OrganList)
					{
						if (organ is Lungs lung)
						{
							lung.TryBreathing(node, 1, true);
							lung.TryBreathing(node, 1, true);
							lung.TryBreathing(node, 1, true);
							hasLung = true;
						}

						if (organ is Heart heart)
						{
							heart.ForcedBeats =+ 6;
							hasHeart = true;
						}
					}
				}
			}

			if (hasLung && hasHeart)
			{
				Chat.AddActionMsgToChat(
					performer,
					$"You perform CPR on {targetName}.",
					$"{performerName} performs CPR on {targetName}.");
				Chat.AddExamineMsgFromServer(target, $"You feel fresh air enter your lungs and your heart pumping, It feels good!");
			}
			else
			{
				Chat.AddActionMsgToChat(
					performer,
					$"You perform CPR on {targetName}. It doesn't seem to work, maybe they're missing something.",
					$"{performerName} performs CPR on {targetName} In vain.");
			}
		}
	}
}