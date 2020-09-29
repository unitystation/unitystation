using UnityEngine;

namespace Disposals
{
	public class DisposalPipeBroken : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField] float cutTime = 3;

		string objectName;
		HandApply currentInteraction;

		void Awake()
		{
			objectName = gameObject.ExpensiveName();
			if (gameObject.TryGetComponent(out ObjectAttributes attributes))
			{
				objectName = attributes.InitialName;
			}
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

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

		void Weld()
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

		void DespawnBrokenPipe()
		{
			Despawn.ServerSingle(this.gameObject);
		}

		#endregion Construction
	}
}
