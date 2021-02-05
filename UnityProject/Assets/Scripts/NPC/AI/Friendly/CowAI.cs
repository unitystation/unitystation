using UnityEngine;

namespace Systems.MobAIs
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