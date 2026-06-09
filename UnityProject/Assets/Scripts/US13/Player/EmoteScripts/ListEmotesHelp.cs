using System.Text;
using UnityEngine;
using US13.Core.Chat;
using US13.ScriptableObjects.RP;

namespace US13.Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/ListAllEmotes")]
	public class ListEmotesHelp : EmoteSO
	{
		public override void Do(GameObject actor)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var emoteFound in EmoteActionManager.Instance.EmoteList.Emotes)
			{
				if (emoteFound is SpeciesSpecificEmote)
				{
					var n = (SpeciesSpecificEmote)emoteFound;
					if(n.IsSameSpecies(actor) == false) continue;
				}
				stringBuilder.Append(emoteFound.EmoteName);
				stringBuilder.Append(",");
			}
			Chat.AddExamineMsg(actor, stringBuilder.ToString());
		}
	}
}