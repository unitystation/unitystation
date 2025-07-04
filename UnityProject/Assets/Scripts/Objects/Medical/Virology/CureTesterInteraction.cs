using UnityEngine;

namespace Objects.Medical.Virology
{
	[RequireComponent(typeof(CureTester))]
	public class CureTesterInteraction : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		[SerializeField] private SpriteClickRegion powerButtonRegion = default;
		[SerializeField] private SpriteClickRegion loadCureRegion = default;

		[SerializeField] private CureTester parentCureTester = default;
		[SerializeField] private ItemTrait reagentContainerItemTrait;

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (loadCureRegion.Contains(interaction.WorldPositionTarget))
			{
				if (interaction.HandObject && Validations.HasItemTrait(interaction, reagentContainerItemTrait) &&
				    parentCureTester.IsFull == false) return true;
				if (interaction.HandObject == false && parentCureTester.IsFull) return true;

				return false;
			}

			if (powerButtonRegion.Contains(interaction.WorldPositionTarget) && parentCureTester.CanExamineSample) return true;

			return false;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (loadCureRegion.Contains(interaction.WorldPositionTarget))
				parentCureTester.RequestLoadRemoveItem(interaction, reagentContainerItemTrait);
			if (powerButtonRegion.Contains(interaction.WorldPositionTarget))
				parentCureTester.RequestExamineCure(interaction);
		}
	}
}