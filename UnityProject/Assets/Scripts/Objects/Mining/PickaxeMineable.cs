using System.Collections;
using UnityEngine;

namespace Objects.Mining
{
	/// <summary>
	/// Simple script to allow objects to be mined. Destroys the object upon completion.
	/// </summary>
	public class PickaxeMineable : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private float mineTime = 3;

		private string objectName;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			objectName = gameObject.ExpensiveName();

			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Pickaxe);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			ToolUtils.ServerUseToolWithActionMessages(
					interaction, mineTime,
					$"You start mining the {objectName}...",
					$"{interaction.Performer.ExpensiveName()} starts mining the {objectName}...",
					default, default,
					() =>
					{
						SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.BreakStone,
								interaction.PerformerPlayerScript.WorldPos, sourceObj: interaction.Performer);
						Despawn.ServerSingle(gameObject);
					});
		}
	}
}
