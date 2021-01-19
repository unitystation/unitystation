using UnityEngine;

namespace Objects.Disposals
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
				objectName = attributes.InitialName;
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
			Despawn.ServerSingle(gameObject);
		}

		#endregion Construction
	}
}
