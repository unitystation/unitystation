using Messages.Client;
using Mirror;

namespace Systems.CraftingV2
{
	public class RequestCraft : ClientMessage<RequestCraft.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int craftingRecipeIndex;
		}

		public override void Process(NetMessage netMessage)
		{
			if (netMessage.craftingRecipeIndex == NetId.Invalid)
			{
				return;
			}

			SentByPlayer.Script.PlayerCrafting.TryToStartCrafting(
				CraftingRecipeSingleton.Instance.StoredCraftingRecipes[netMessage.craftingRecipeIndex]
			);
		}

		public static void Send(CraftingRecipe craftingRecipe)
		{
			Send(new NetMessage
			{
				craftingRecipeIndex = CraftingRecipeSingleton.Instance.StoredCraftingRecipes.IndexOf(craftingRecipe)
			});
		}
	}
}