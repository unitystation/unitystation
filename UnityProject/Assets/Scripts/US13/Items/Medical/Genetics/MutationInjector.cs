using System.Collections.Generic;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.HealthV2.Living;
using US13.Items.Tool;
using US13.Objects.Medical;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Items.Medical.Genetics
{
	public class MutationInjector : Syringe
	{
		public List<DNAMutationData> DNAPayload = new List<DNAMutationData>();
		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if (LHB != null)
			{
				LHB.InjectDna(DNAPayload);
				SpriteHandler.SetCatalogueIndexSprite(1);
			}
		}

		public override void InjectBehavior(LivingHealthMasterBase LHB, RegisterPlayer performer)
		{
			Chat.AddCombatMsgToChat(performer.gameObject,
				$"You Inject The {this.name} into {LHB.gameObject.ExpensiveName()}",
				$"{performer.PlayerScript.visibleName} injects a {this.name} into {LHB.gameObject.ExpensiveName()}");
			LHB.InjectDna(DNAPayload);

			SpriteHandler.SetCatalogueIndexSprite(1);
		}
	}
}