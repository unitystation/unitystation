using System.Text;
using Core.Chat;
using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/ListAllEmotes")]
	public class ListEmotesHelp : EmoteSO
	{
		public override void Do(GameObject player)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var emoteFound in EmoteActionManager.Instance.EmoteList.Emotes)
			{
				if (emoteFound is SpeciesSpecificEmote)
				{
					var n = (SpeciesSpecificEmote)emoteFound;
					if(n.IsSameSpecies(player) == false) continue;
				}
				stringBuilder.Append(emoteFound.EmoteName);
				stringBuilder.Append(",");
			}
			Chat.AddExamineMsg(player, stringBuilder.ToString());
		}
	}
}