using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RemoteSyntheticControlConsole : MonoBehaviour
{

	private RegisterTile RegisterTile;
	public List<RemotelyControlledBrain> CyborgsOnMatrix = new List<RemotelyControlledBrain>();

	private int UpdatedPlayerFrame = 0;

	public void Awake()
	{
		RegisterTile = this.GetComponent<RegisterTile>();
	}

	private void OnEnable()
	{
		UpdateManager.Add(UpdateCyborgsOnMatrix, 1);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE ,UpdateCyborgsOnMatrix);
	}



	public void UpdateCyborgsOnMatrix()
	{
		if (UpdatedPlayerFrame != RegisterTile.Matrix.UpdatedPlayerFrame)
		{
			UpdatedPlayerFrame = RegisterTile.Matrix.UpdatedPlayerFrame;
			CyborgsOnMatrix.Clear();
			foreach (var Player in RegisterTile.Matrix.PresentPlayers)
			{
				var Brain = Player.OrNull()?.PlayerScript.OrNull()?.playerHealth.OrNull()?.brain;
				if (Brain != null && Brain.TryGetComponent<RemotelyControlledBrain>(out var RemotelyControlledBrain))
				{
					CyborgsOnMatrix.Add(RemotelyControlledBrain);
				}
			}
		}
	}
}
