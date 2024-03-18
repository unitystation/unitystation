using System.Collections;
using UnityEngine;

/// <summary>
/// Throw this bad boy on any shuttle that needs initial movement
/// on round start
/// </summary>
public class ShuttleStartAutoMove : MonoBehaviour
{
	[SerializeField] private float SetInitialSpeed = 10;
	[SerializeField] private bool ShuttleSafetyEnabled = false;
	[SerializeField] private float RoundStartDelay = 0f;

	private void OnEnable()
	{
		EventManager.AddHandler(Event.RoundStarted, DoAutoMove);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(Event.RoundStarted, DoAutoMove);
	}

	void DoAutoMove()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			var matrixMove = GetComponent<MatrixMove>();
			if (matrixMove != null)
			{
				StartCoroutine(TryAutoMove(matrixMove));
			}
		}
	}

	IEnumerator TryAutoMove(MatrixMove matrixMove)
	{
		yield return WaitFor.Seconds(RoundStartDelay);

		matrixMove.NetworkedMatrixMove.Drag = 0;
		matrixMove.NetworkedMatrixMove.DragTorque = 0;
		matrixMove.NetworkedMatrixMove.TileAlignmentSpeed = 0;
		matrixMove.NetworkedMatrixMove.LowSpeedDrag = 0;
		matrixMove.NetworkedMatrixMove.SpinneyThreshold = 0;
		matrixMove.NetworkedMatrixMove.WorldCurrentVelocity = matrixMove.NetworkedMatrixMove.ForwardsDirection * SetInitialSpeed;
	}
}
