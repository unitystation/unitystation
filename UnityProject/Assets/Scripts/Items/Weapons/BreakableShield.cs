using Items;
using UnityEngine;

namespace Weapons
{
	public class BreakableShield : MonoBehaviour, ICheckedInteractable<InventoryApply>
	{
		private Integrity integ;
		private ItemAttributesV2 attribs;
		protected StandardProgressActionConfig ProgressConfig
			= new(StandardProgressActionType.ItemTransfer);

		[SerializeField] private bool allowRepair;
		[SerializeField] private float repairTime = 2f;
		[SerializeField] private ItemTrait repairTrait;

		private void Awake()
		{
			integ = GetComponent<Integrity>();
			attribs = GetComponent<ItemAttributesV2>();
			attribs.OnBlock += DamageOnBlock;
		}

		private void OnDestroy()
		{
			attribs.OnBlock -= DamageOnBlock;
		}

		public void DamageOnBlock(GameObject attacker, float damage, DamageType damageType)
		{
			integ.ApplyDamage(damage, AttackType.Melee, damageType);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (allowRepair == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject == gameObject && Validations.HasItemTrait(interaction.UsedObject, repairTrait))
			{
				return true;
			}

			return false;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, repairTrait) && integ.integrity < integ.initialIntegrity)
			{
				void ProgressFinishAction()
				{
					string usedObjectName = interaction.UsedObject.ExpensiveName();
					if (interaction.UsedObject.TryGetComponent<Stackable>(out var stackable) && stackable.Amount > 1)
					{
						stackable.ServerConsume(1);
					} else
					{
						Inventory.ServerDespawn(interaction.UsedObject);
					}

					Chat.AddExamineMsgFromServer(interaction.Performer, $"You repair {gameObject.ExpensiveName()} with {usedObjectName}.");
					integ.RestoreIntegrity(integ.initialIntegrity);
				}

				var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
					.ServerStartProgress(interaction.Performer.RegisterTile(), repairTime, interaction.Performer);
			}
		}
	}
}