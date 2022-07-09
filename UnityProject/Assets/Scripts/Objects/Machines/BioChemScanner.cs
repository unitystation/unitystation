using System.Text;
using UnityEngine;

namespace Objects.Machines
{
	public class BioChemScanner : MonoBehaviour, ICheckedInteractable<HandApply>
	{

		[SerializeField] private float scanTime = 3f;
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasComponent<Syringe>(interaction.HandObject) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(interaction.HandObject.TryGetComponent<Syringe>(out var syringe) == false) return;

			void Scan()
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (var sickness in syringe.SicknessesInSyringe)
				{
					stringBuilder.AppendLine("-----");
					stringBuilder.AppendLine($"- Name : {sickness.Sickness.SicknessName}");
					stringBuilder.AppendLine($"- Estimated First Exposure Time : {sickness.ContractedTime}");
					stringBuilder.AppendLine($"- Possible reagents that can lead to cure: ");
					foreach (var hint in sickness.Sickness.CureHints)
					{
						stringBuilder.AppendLine($"-- {hint.Name} | {hint.heatDensity}'c");
					}
				}
				Chat.AddExamineMsg(interaction.Performer, stringBuilder.ToString());
				syringe.SicknessesInSyringe.Clear();
			}
			var action = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.SelfHeal), Scan)
				.ServerStartProgress(gameObject.AssumedWorldPosServer(), scanTime, interaction.Performer);
		}
	}
}