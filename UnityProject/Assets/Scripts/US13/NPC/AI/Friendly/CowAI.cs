using UnityEngine;
using US13.Core.Chat;

namespace US13.NPC.AI.Friendly
{
	public class CowAI: GenericFriendlyAI
	{
		[SerializeField] private string noMoreMilkMessage = "You pull the teat but no more milk is coming out!";

		public void SendNoMilkMessage(GameObject cow, GameObject performer)
		{
			Chat.AddExamineMsg(performer, noMoreMilkMessage);
		}
	}
}