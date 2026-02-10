using UnityEngine;
using US13.Managers;

namespace US13.Items.Others.Magical.SpellBooks
{
	/// <summary>
	/// Allows punishment to be inflicted upon the reader of a depleted spell book.
	/// </summary>
	public abstract class SpellBookPunishment : MonoBehaviour
	{
		public abstract void Punish(PlayerInfo player);
	}
}
