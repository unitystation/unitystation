using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Items;
using US13.Items.Tool;
using Util;

namespace US13.Objects.Disposals
{
	public class DisposalPipeBroken : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private float cutTime = 3;

		private string objectName;
		private HandApply currentInteraction;

		private void Awake()
		{
			objectName = gameObject.ExpensiveName();
			if (gameObject.TryGetComponent<ObjectAttributes>(out var attributes))
			{
				objectName = attributes.ArticleName;
			}
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasUsedActiveWelder(interaction);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			currentInteraction = interaction;

			if (Validations.HasUsedActiveWelder(interaction))
			{
				Weld();
			}
		}

		#endregion Interactions

		#region Construction

		private void Weld()
		{
			ToolUtils.ServerUseToolWithActionMessages(
					currentInteraction, cutTime,
					$"You start slicing off the {objectName}...",
					$"{currentInteraction.Performer.ExpensiveName()} starts slicing off the {objectName}...",
					$"You remove the {objectName}.",
					$"{currentInteraction.Performer.ExpensiveName()} removes the {objectName}.",
					() => DespawnBrokenPipe()
			);
		}

		private void DespawnBrokenPipe()
		{
			_ = Despawn.ServerSingle(gameObject);
		}

		#endregion Construction
	}
}
