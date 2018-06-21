using UnityEngine;
/// Server only stuff
public class GUI_ShuttleControl : NetTab {
	private MatrixMove matrixMove;
	private MatrixMove MatrixMove {
		get {
			if ( !matrixMove ) {
				matrixMove = Provider.GetComponent<ShuttleInteract>().ShuttleMatrixMove;
			}

			return matrixMove;
		}
//		set { matrixMove = value; }
	}

	/// <summary>
	/// Starts or stops the shuttle.
	/// </summary>
	/// <param name="off">Toggle parameter</param>
	public void TurnOnOff( bool on ) {
		if ( on ) {
			MatrixMove.StartMovement();
		} else {
			MatrixMove.StopMovement();
		}
	}

	/// <summary>
	/// Turns the shuttle right.
	/// </summary>
	public void TurnRight() {
		MatrixMove.TryRotate( true );
	}

	/// <summary>
	/// Turns the shuttle left.
	/// </summary>
	public void TurnLeft() {
		MatrixMove.TryRotate( false );
	}

	/// <summary>
	/// Sets shuttle speed.
	/// </summary>
	/// <param name="speedMultiplier"></param>
	public void SetSpeed( float speedMultiplier ) {
		float speed = speedMultiplier * ( MatrixMove.maxSpeed - 1 ) + 1;
		Debug.Log( $"Multiplier={speedMultiplier}, setting speed to {speed}" );
		MatrixMove.SetSpeed( speed );
	}
}