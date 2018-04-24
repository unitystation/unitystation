using UnityEngine;

public class TransformGizmoDrawer : MonoBehaviour {
	private CustomNetTransform cnt;
	private void Start() {
		cnt = GetComponent<CustomNetTransform>();
	}
#if UNITY_EDITOR
	//Visual debug
//	private Vector3 size1 = Vector3.one;
	private Vector3 size2 = new Vector3( 0.9f, 0.9f, 0.9f );
//	private Vector3 size3 = new Vector3( 0.8f, 0.8f, 0.8f );
	private Vector3 size4 = new Vector3( 0.7f, 0.7f, 0.7f );
//	private Color color1 = Color.red;
	private Color color2 = DebugTools.HexToColor( "fd7c6e" );
//	private Color color3 = DebugTools.HexToColor( "22e600" );
	private Color color4 = DebugTools.HexToColor( "ebfceb" );

	private void OnDrawGizmos() {
		if ( !cnt ) {
			return;
		}
		//serverState
		Gizmos.color = color2;
		Vector3 ssPos = cnt.ServerState.WorldPosition;
		Gizmos.DrawWireCube( ssPos, size2 );
		GizmoUtils.DrawArrow( ssPos + Vector3.right / 2, cnt.ServerState.Impulse );
		GizmoUtils.DrawText( cnt.ServerState.MatrixId.ToString(), ssPos + Vector3.right / 2 + Vector3.up / 3, 15 );

		//client playerState
		Gizmos.color = color4;
		Vector3 clientState = cnt.ClientState.WorldPosition;
		Gizmos.DrawWireCube( clientState, size4 );
		GizmoUtils.DrawArrow( clientState + Vector3.right / 5, cnt.ClientState.Impulse );
		GizmoUtils.DrawText( cnt.ClientState.MatrixId.ToString(), clientState + Vector3.right / 2 + Vector3.up / 6, 15 );
	}
#endif
}