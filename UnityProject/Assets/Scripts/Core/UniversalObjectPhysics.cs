using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Mirror;
using Objects;
using UnityEngine;

public class UniversalObjectPhysics : MonoBehaviour
{

	public Vector2 NewtonianMomentum; //* attributes.Size -> weight


	private Attributes attributes;
	private RegisterTile registerTile;

	public void Awake()
	{

		attributes = GetComponent<Attributes>();
		registerTile = GetComponent<RegisterTile>();
	}


	public void TryTilePush(Vector2Int WorldDirection, float speed = Single.NaN)
	{
		//Validate
		ForceTilePush(WorldDirection, speed);
	}

	public void ForceTilePush(Vector2Int WorldDirection, float speed = Single.NaN)
	{
		//move
	}


	public void NewtonianPush(Vector2Int WorldDirection, float speed = Single.NaN) //Conclusion is just naturally part of Newtonian push
	{
	}


	//--Handles--
	//pushing
	//IS Gravity
	//space movement/Slipping
	//Pulling
}