using System;
using UnityEngine;

/// Put this on items with CustomNetTransform and you'll have gizmos for these
public class TransformGizmoDrawer : MonoBehaviour {
	private UniversalObjectPhysics UOP;
	private RegisterTile rt;
	private void Start() {
		UOP = GetComponent<UniversalObjectPhysics>();
		rt = GetComponent<RegisterTile>();
	}
#if UNITY_EDITOR
	//Visual debug
	[NonSerialized]
	private readonly Vector3 size1 = Vector3.one,
							 size2 = new Vector3( 0.9f, 0.9f, 0.9f ),
							 size3 = new Vector3( 0.8f, 0.8f, 0.8f ),
							 size4 = new Vector3( 0.7f, 0.7f, 0.7f );
	[NonSerialized]
	private readonly Color  color0 = DebugTools.HexToColor( "5566ff55" ),//blue
							color1 = Color.red,
							color2 = DebugTools.HexToColor( "fd7c6e" ),//pink
							color3 = DebugTools.HexToColor( "22e600" ),//green
							color4 = DebugTools.HexToColor( "ebfceb" ),//white
							color7 = DebugTools.HexToColor( "ff666655" );//reddish
	private void OnDrawGizmos() {
		if ( !UOP ) {
			return;
		}
		//registerTile server pos
		Gizmos.color = color7;
		Vector3 regPosS = rt.WorldPositionServer;
		Gizmos.DrawCube( regPosS, size1 );

		//registerTile client pos
		Gizmos.color = color0;
		Vector3 regPosC = rt.WorldPosition;
		Gizmos.DrawCube( regPosC, size2 );


		//serverState
		Gizmos.color = color2;
		Vector3 ssPos = UOP.OfficialPosition;
		Gizmos.DrawWireCube( ssPos, size2 );
		DebugGizmoUtils.DrawArrow( ssPos + Vector3.right / 2, UOP.newtonianMovement );
		DebugGizmoUtils.DrawText( UOP.registerTile.Matrix.Id.ToString(), ssPos + Vector3.right / 2 + Vector3.up / 3, 15 );

	}
#endif
}