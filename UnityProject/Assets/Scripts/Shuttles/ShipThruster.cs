using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThruster : MonoBehaviour
{

	public MatrixMove shipMatrixMove; // ship matrix move
	public ParticleSystem particleFX;

	public float particleSpeedMultiplier = 1.5f;

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
		shipMatrixMove.OnStart.RemoveListener(UpdateEngineState);
		shipMatrixMove.OnStop.RemoveListener(UpdateEngineState);
		shipMatrixMove.OnRotate.RemoveListener(RotateFX);
		shipMatrixMove.OnSpeedChange.RemoveListener(SpeedChange);
	}

	IEnumerator Init()
	{
		int tries = 0;
		while (shipMatrixMove == null)
		{
			tries++;
			shipMatrixMove = transform.root.gameObject.GetComponent<MatrixMove>();
			yield return new WaitForEndOfFrame();
			if(tries >= 5){
				this.enabled = false;
				yield break;
			}

		}
		yield return new WaitForEndOfFrame();
		shipMatrixMove.OnStart.AddListener(UpdateEngineState);
		shipMatrixMove.OnStop.AddListener(UpdateEngineState);
		shipMatrixMove.OnRotate.AddListener(RotateFX);
		shipMatrixMove.OnSpeedChange.AddListener(SpeedChange);
	}

	public void UpdateEngineState()
	{
		var emissionFX = particleFX.emission;
		if (EngineStatus())
		{
			emissionFX.enabled = true;
			SpeedChange(0, shipMatrixMove.ClientState.Speed, particleSpeedMultiplier); //Set particle speed on engine updates, used for setting speed at beginning of flight.
		}
		else
		{
			emissionFX.enabled = false;
		}
	}

	//Rotates FX as ship rotates
	public void RotateFX(RotationOffset newRotationOffset)
	{
		var mainFX = particleFX.main;

		mainFX.startRotation = newRotationOffset.Degree * Mathf.Deg2Rad;
	}

	public void SpeedChange(float oldSpeed, float newSpeed, float _particleSpeedMultiplier)
	{
		var mainFX = particleFX.main;
		particleSpeedMultiplier = _particleSpeedMultiplier;

		mainFX.startSpeed = newSpeed * _particleSpeedMultiplier;
	}

	public void SpeedChange(float oldSpeed, float newSpeed)
	{
		var mainFX = particleFX.main;

		mainFX.startSpeed = newSpeed * particleSpeedMultiplier;
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
