using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;

public class EventRouter : MonoBehaviour, INewMappedOnSpawn
{
	//TODO Specifying data sometime
	public List<EventConnection> EventLinks = new List<EventConnection>();


	public void OnNewMappedOnSpawn()
	{
		foreach (var EventLink in EventLinks)
		{
			AllowedReflection.PopulateEventRouter(EventLink);
		}
	}
}