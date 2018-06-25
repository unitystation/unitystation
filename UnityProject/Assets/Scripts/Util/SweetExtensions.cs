using System;
using System.Collections.Generic;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

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

		public static NetworkInstanceId NetId( this GameObject go ) {
			return go ? go.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid;
		}

		public static Vector3 WorldPos( this GameObject go ) {
			return go.GetComponent<RegisterTile>()?.WorldPosition ?? go.transform.position;
//			return go.GetComponent<CustomNetTransform>()?.State.position ?? go.Player()?.Script.playerSync.ServerState.WorldPosition ??  go.transform.position;
		}
		
		/// Wraps provided index value if it's more that array length  
		public static T Wrap<T>(this T[] array, int index)
		{
			return array[((index % array.Length) + array.Length) % array.Length];
		}
	}
}