using Chemistry;
using UnityEngine;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Items;
using US13.Items.Traits;
using US13.Player;
using US13.Systems.Antagonists;

namespace US13.HealthV2.Living.MedicalChemistry
{
	[CreateAssetMenu(fileName = "vampirismEffect",
		menuName = "ScriptableObjects/Chemistry/Vampirism")]
	public class VampirismEffect : Chemistry.Effect
	{
		[SerializeField] private ItemTrait requiredTrait = null;

		public override void Apply(MonoBehaviour sender, ReagentMix reagentMix, Vector3 worldPosition, float amount)
		{
			if (sender == null) return;
			if (sender.TryGetComponent<ItemAttributesV2>(out var attributes) == false) return;
			if (requiredTrait == true && attributes.HasTrait(requiredTrait) == false) return;


			var metabolismComponent = sender as MetabolismComponent;
			if (metabolismComponent == false) return;

			PlayerScript playerScript = metabolismComponent.AssociatedSystem?.Base?.playerScript;
			if (playerScript == false) return;


			if(playerScript.TryGetComponent<VampireStageProgression>(out var vampireStageProgression) == false) return;
			vampireStageProgression.Apply();
		}
	}
}
