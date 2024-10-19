using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A basic interface for if you want an object to act as the controller for multiple actions, action ownership should still be handled IGameActionContainer
/// </summary>
public interface IMultiGameActionController
{
	/// <summary>
	/// The dictionary of our action objects keyed to their GUID
	/// </summary>
	Dictionary<string, GameActionObject> ActionDataDict { get; }

	//the recommended way to handle this is to set each GameActionObject to its own unique serialized field and then put them into ActionDataDict in here
	Dictionary<string, GameActionObject> GenerateActionDataDict(List<GameActionObject> addedData)
	{
		Dictionary<string, GameActionObject> newDict = new();
		foreach(GameActionObject obj in addedData)
		{
			newDict[Guid.NewGuid().ToString()] = obj;
		}
		return newDict;
	}
}
