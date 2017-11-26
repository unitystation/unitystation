using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterPlayer : RegisterTile
	{
		void Update () {
			var currentPos = Vector3Int.FloorToInt(transform.localPosition);
			
			if (!currentPos.Equals(position))
			{
				position = currentPos;
			}
		}
	}
}
