using Systems.Disposals;
using UnityEngine;

namespace Core.InputSystem.InteractionV2
{
	[CreateAssetMenu(fileName = "CrawlIntoPipe",
		menuName = "Interaction/TileInteraction/CrawlIntoPipe")]
	public class CrawlIntoPipeInteraction : TileInteraction
	{
		public override bool WillInteract(TileApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public override void ServerPerformInteraction(TileApply interaction)
		{
			if (interaction.HandObject is not null) return;
			global::Chat.AddActionMsgToChat(interaction.Performer,
				$"{interaction.Performer.ExpensiveName()} crawls into the pipe.");
			DisposalsManager.Instance.NewDisposal(interaction.Performer);
		}
	}
}