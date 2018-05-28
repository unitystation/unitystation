using System;
using System.Collections.Generic;
using UnityEngine;

namespace Util {
	public static class SweetExtensions {
		public static ConnectedPlayer Player( this GameObject go ) {
			var connectedPlayer = PlayerList.Instance?.Get( go );
			return connectedPlayer == ConnectedPlayer.Invalid ? null : connectedPlayer;
		}
		public static ItemAttributes Item( this GameObject go ) {
			return go.GetComponent<ItemAttributes>();
		}

		public static string ExpensiveName( this GameObject go ) {
			return go.Player()?.Name ?? go.Item()?.itemName ?? go.name.Replace( "NPC_", "" ).Replace( "_", " " );
		}

		public static T GetRandom<T>( this List<T> list ) {
			return list?.Count > 0 ? list.PickRandom() : default(T);
		}
	}
}