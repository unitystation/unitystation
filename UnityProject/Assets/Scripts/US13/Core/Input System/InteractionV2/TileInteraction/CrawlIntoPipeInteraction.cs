using UnityEngine;
using US13.Systems.Disposals;
using Util;

namespace US13.Core.Input_System.InteractionV2.TileInteraction
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
			global::US13.Core.Chat.Chat.AddActionMsgToChat(interaction.Performer,
				$"{interaction.Performer.ExpensiveName()} crawls into the pipe.");
			DisposalsManager.Instance.NewDisposal(interaction.Performer);
		}
	}
}