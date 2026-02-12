using UnityEngine;
using US13.Core.Chat;
using US13.Player;
using US13.Systems.Spells;

namespace US13.Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Swapper")]
	public class Swapper : Antagonist
	{
		public SpellData SwapSpell;

		public override void AfterSpawn(Mind player)
		{
			Spell spell = SwapSpell.AddToPlayer(player);
			player.AddSpell(spell);
			Chat.AddExamineMsgFromServer(player.gameObject, "You are the Swap with as Many people you can");
		}
	}
}