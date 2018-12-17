using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

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

		/// Creates garbage! Use very sparsely!
		public static Vector3 WorldPos( this GameObject go ) {
			return go.GetComponent<RegisterTile>()?.WorldPosition ?? go.transform.position;
//			return go.GetComponent<CustomNetTransform>()?.State.position ?? go.Player()?.Script.playerSync.ServerState.WorldPosition ??  go.transform.position;
		}
		/// Creates garbage! Use very sparsely!
		public static RegisterTile RegisterTile( this GameObject go ) {
			return go.GetComponent<RegisterTile>();
		}

		/// Wraps provided index value if it's more that array length
		public static T Wrap<T>(this T[] array, int index)
		{
			return array[((index % array.Length) + array.Length) % array.Length];
		}

		public static BoundsInt BoundsAround( this Vector3Int pos ) {
			return new BoundsInt( pos - new Vector3Int( 1, 1, 0 ), new Vector3Int( 3, 3, 1 ) );
		}

		private const float NO_BOOST_THRESHOLD = 1.5f;

		/// Lerp speed modifier
		public static float SpeedTo( this Vector3 lerpFrom, Vector3 lerpTo ) {
			float distance = Vector2.Distance( lerpFrom, lerpTo );
			if ( distance <= NO_BOOST_THRESHOLD ) {
				return 1;
			}

			float boost = (distance - NO_BOOST_THRESHOLD) * 2;
			if ( boost > 0 ) {
				Logger.LogTraceFormat( "Lerp speed boost exceeded by {0}", Category.Lerp, boost );
			}
			return 1 + boost;
		}

		/// Serializing Vector2 (rounded to int) into plaintext
		public static string Stringified( this Vector2 pos ) {
			return ( int ) pos.x+"x"+( int ) pos.y;
		}

		/// Deserializing Vector2(Int) from plaintext.
		/// In case of parse error returns HiddenPos
		public static Vector3 Vectorized( this string stringifiedVector ) {
			var posData = stringifiedVector.Split( 'x' );
			int x, y;
			if ( posData.Length > 1 && int.TryParse(posData[0], out x) && int.TryParse(posData[1], out y) ) {
				return new Vector2(x, y);
			}
			Logger.LogWarning( $"Vector parse failed: what the hell is '{stringifiedVector}'?" );
			return TransformState.HiddenPos;
		}

		/// Good looking job name
		public static string JobString(this JobType job)
		{
			return job.ToString().Equals("NULL") ? "*just joined" : textInfo.ToTitleCase(job.ToString().ToLower());
		}
		//For job formatting purposes
		private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
	}