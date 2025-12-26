using ScriptableObjects.Systems.Spells;
using Systems.Spells;
using UnityEngine;

namespace Antagonists
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