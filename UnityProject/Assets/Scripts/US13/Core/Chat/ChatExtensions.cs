using UnityEngine;

namespace US13.Core.Chat
{
	public static class ChatExtensions
	{
		/// <summary>
		/// Helper function that calls Chat.AddActionMsgToChat with the given message and this gameobject as the originator.
		/// </summary>
		/// <param name="originator"></param>
		/// <param name="everyoneMessage"></param>
		public static void AddActionMsgToChat(this GameObject originator, string everyoneMessage)
		{
			Chat.AddActionMsgToChat(originator, everyoneMessage);
		}
	}
}