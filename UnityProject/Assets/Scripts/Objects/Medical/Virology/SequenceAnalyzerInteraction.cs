using UnityEngine;

namespace Objects.Medical.Virology
{
	[RequireComponent(typeof(SequenceAnalyzer))]
	public class SequenceAnalyzerInteraction : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		[SerializeField] private SpriteClickRegion powerButtonRegion = default;

		[SerializeField] private SpriteClickRegion loadSlideRegion = default;

		[SerializeField] private SequenceAnalyzer parentAnalyzer;

		[SerializeField] private ItemTrait dishItemTrait;

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (loadSlideRegion.Contains(interaction.WorldPositionTarget))
			{
				if (interaction.HandObject && Validations.HasItemTrait(interaction, dishItemTrait) &&
				    parentAnalyzer.HasDishLoaded == false)
					return
						true;
				if (interaction.HandObject == false && parentAnalyzer.HasDishLoaded)
					return
						true;

				return false;
			}

			if (powerButtonRegion.Contains(interaction.WorldPositionTarget) && parentAnalyzer.CanExamineSample) return true;

			return false;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (loadSlideRegion.Contains(interaction.WorldPositionTarget))
				parentAnalyzer.RequestLoadRemoveDish(interaction);
			if (powerButtonRegion.Contains(interaction.WorldPositionTarget))
				parentAnalyzer.RequestExamineDish();
		}
	}
}