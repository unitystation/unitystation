using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Objects.Medical;

namespace US13.Items.Medical.Genetics
{
	public class Amber : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		public Stackable Stacking;

		public void Start()
		{
			Stacking = this.GetComponent<Stackable>();
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject == gameObject) return false;
			if ( Validations.HasComponent<DNAConsole>(interaction.TargetObject) == false) return false;
			return true;
		}


		public void ServerPerformInteraction(PositionalHandApply interaction)
		{

			var DNAConsole = interaction.TargetObject.GetComponent<DNAConsole>();
			if (DNAConsole != null)
			{
				if (DNAConsole.AddAmber())
				{
					if (Stacking != null)
					{
						Stacking.ServerConsume(1);
					}
					else
					{
						_ = Despawn.ServerSingle(gameObject);
					}
				}
			}
		}
	}
}
