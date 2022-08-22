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
		while (!matrixMove.Initialized)
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.Seconds(RoundStartDelay);

		matrixMove.SetSpeed(SetInitialSpeed);
		matrixMove.SafetyProtocolsOn = ShuttleSafetyEnabled;
		matrixMove.RequiresFuel = false;
		matrixMove.StartMovement();
	}
}
