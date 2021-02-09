using UnityEngine;
using System.Collections;

namespace Items.Others.Magical
{
	/// <summary>
	/// Allows punishment to be inflicted upon the reader of a depleted spell book.
	/// </summary>
	public abstract class SpellBookPunishment : MonoBehaviour
	{
		public abstract void Punish(ConnectedPlayer player);
	}
}
