using System;
using UnityEngine;

/// Put this on items with CustomNetTransform and you'll have gizmos for these
public class TransformGizmoDrawer : MonoBehaviour {
	private CustomNetTransform cnt;
	private RegisterTile rt;
	private void Start() {
		cnt = GetComponent<CustomNetTransform>();
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
		if ( !cnt ) {
			return;
		}
		//registerTile server pos
		Gizmos.color = color7;
		Vector3 regPosS = rt.WorldPositionServer;
		Gizmos.DrawCube( regPosS, size1 );

		//registerTile client pos
		Gizmos.color = color0;
		Vector3 regPosC = rt.WorldPositionClient;
		Gizmos.DrawCube( regPosC, size2 );

		//server lerp
		Gizmos.color = color1;
		Vector3 stPos = cnt.ServerLerpState.WorldPosition;
		Gizmos.DrawWireCube( stPos, size1 );

		//serverState
		Gizmos.color = color2;
		Vector3 ssPos = cnt.ServerState.WorldPosition;
		Gizmos.DrawWireCube( ssPos, size2 );
		DebugGizmoUtils.DrawArrow( ssPos + Vector3.right / 2, cnt.ServerState.WorldImpulse );
		DebugGizmoUtils.DrawText( cnt.ServerState.MatrixId.ToString(), ssPos + Vector3.right / 2 + Vector3.up / 3, 15 );

		//predictedState
		Gizmos.color = color3;
		Vector3 predictedState = cnt.PredictedState.WorldPosition;
		Gizmos.DrawWireCube( predictedState, size4 );
		DebugGizmoUtils.DrawArrow( predictedState + Vector3.right / 5, cnt.PredictedState.WorldImpulse );
		DebugGizmoUtils.DrawText( cnt.PredictedState.MatrixId.ToString(), predictedState + Vector3.right / 2 + Vector3.up / 6, 15 );
//		GizmoUtils.DrawText( cnt.ClientState.Speed.ToString(), clientState + Vector3.right / 1.5f + Vector3.up / 6, 10 );

		//clientState
		Gizmos.color = color4;
		Vector3 clientState = cnt.ClientState.WorldPosition;
		Gizmos.DrawWireCube( clientState, size3 );
		DebugGizmoUtils.DrawArrow( clientState + Vector3.right / 5, cnt.ClientState.WorldImpulse );
//		GizmoUtils.DrawText( cnt.PredictedState.MatrixId.ToString(), clientState + Vector3.right / 2 + Vector3.up / 6, 15 );
	}
#endif
}