using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents brain data for a living entity
/// Use this only on the server, all brain fields on client 
/// will be empty
/// </summary>
[Serializable]
public class Brain
{
	/// <summary>
	/// GUID of the brain, this cannot be changed once it is set
	/// Can be used to find a players brain again if they reconnect to a round
	/// </summary>
	public string GUID { get; private set; }
	public bool IsInBody { get; set; }
	public bool IsDead { get; set; }
	/// <summary>
	/// Amount of brain damage caused to this brain
	/// </summary>
	/// <value>0% to 100%</value>
	public int BrainDamage { get; set; } = 0;
	public GameObject Body { get; set; } = null;

	/// <summary>
	/// A list of brain infections affecting this brain
	/// </summary>
	public List<BrainInfection> brainInfections = new List<BrainInfection>();

	/// <summary>
	/// Create a new brain and randomly set its GUID
	/// Remember to call ConnectToBody when instantiated
	/// </summary>
	public Brain()
	{
		GUID = System.Guid.NewGuid().ToString();
		ResetValues();
	}

	/// <summary>
	/// Call this when you need to connect a brain to a bodies brain system
	/// </summary>
	/// <param name="entity"> The root game object of the player / animal / borg </param>
	public void ConnectBrainToBody(GameObject entity)
	{
		IsInBody = true;
		Body = entity;
	}

	/// <summary>
	/// Call this after removing a brain from a body
	/// Attach this brain to any brain prefab that was spawned
	/// </summary>
	public void RemoveFromBody()
	{
		IsInBody = false;
		Body = null;
	}

	private void ResetValues()
	{
		BrainDamage = 0;
		IsInBody = false;
		IsDead = false;
		brainInfections.Clear();
	}
}