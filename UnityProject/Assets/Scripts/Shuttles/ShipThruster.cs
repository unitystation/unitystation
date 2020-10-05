using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThruster : MonoBehaviour
{

	public MatrixMove shipMatrixMove; // ship matrix move
	public ParticleSystem particleFX;

	public float particleRateMultiplier = 4f;

	void Awake()
	{
		//Gets ship matrix move by getting root (top parent) of current gameobject
		shipMatrixMove = transform.root.gameObject.GetComponent<MatrixMove>();
		particleFX = GetComponentInChildren<ParticleSystem>();
	}

	private void OnEnable()
	{
		StartCoroutine(Init());
	}

	private void OnDisable()
	{
		if (shipMatrixMove == null)
		{
			return;
		}
		shipMatrixMove.MatrixMoveEvents.OnStartMovementClient.RemoveListener(UpdateEngineState);
		shipMatrixMove.MatrixMoveEvents.OnStopMovementClient.RemoveListener(UpdateEngineState);
		shipMatrixMove.MatrixMoveEvents.OnRotate.RemoveListener(RotateFX);
	}

	IEnumerator Init()
	{
		int tries = 0;
		while (shipMatrixMove == null)
		{
			tries++;
			shipMatrixMove = transform.root.gameObject.GetComponent<MatrixMove>();
			yield return WaitFor.EndOfFrame;
			if(tries >= 5){
				this.enabled = false;
				yield break;
			}

		}
		yield return WaitFor.EndOfFrame;
		shipMatrixMove.MatrixMoveEvents.OnStartMovementClient.AddListener(UpdateEngineState);
		shipMatrixMove.MatrixMoveEvents.OnStopMovementClient.AddListener(UpdateEngineState);
		//TODO: Refactor to use Directional
		shipMatrixMove.MatrixMoveEvents.OnRotate.AddListener(RotateFX);
		shipMatrixMove.MatrixMoveEvents.OnSpeedChange.AddListener(SpeedChange);
	}

	public void UpdateEngineState()
	{
		var emissionFX = particleFX.emission;

		// don't enable FX if movement is caused by RCS
		if(shipMatrixMove.rcsModeActive) 
		{
			emissionFX.enabled = false;
		}
		else if (EngineStatus())
		{
			emissionFX.enabled = true;
			SpeedChange(0, shipMatrixMove.ClientState.Speed); //Set particle speed on engine updates, used for setting speed at beginning of flight.
		}
		else
		{
			emissionFX.enabled = false;
		}
	}

	//Rotates FX as ship rotates
	public void RotateFX(MatrixRotationInfo info)
	{
		var mainFX = particleFX.main;

		//mainFX.startRotation = newRotationOffset.Degree * Mathf.Deg2Rad;
	}

	public void SpeedChange(float oldSpeed, float newSpeed)
	{
		var emission = particleFX.emission;

		emission.rateOverTime = Mathf.Clamp(newSpeed * particleRateMultiplier, 30f, 70f);
	}

	bool EngineStatus() // Returns if engines are "on" (if ship is moving)
	{
		if (shipMatrixMove != null)
		{
			return shipMatrixMove.ClientState.IsMoving;
		}

		return false;
	}

}
