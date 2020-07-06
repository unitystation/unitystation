using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuttleTeleport : MonoBehaviour
{
	public Vector2 TeleportCoordinate;

	private MatrixMove matrixMove;

	private void Awake()
	{
		matrixMove = GetComponent<MatrixMove>();
	}

	public void TeleportShuttle()
	{
		matrixMove.SetPosition(TeleportCoordinate);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			TeleportShuttle();
		}
	}
}
