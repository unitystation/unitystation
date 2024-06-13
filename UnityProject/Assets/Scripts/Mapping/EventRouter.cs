using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;

public class EventRouter : MonoBehaviour
{
	//TODO Specifying data sometime
	public List<EventConnection> EventLinks = new List<EventConnection>();

	// Start is called before the first frame update
	void Start()
	{
		foreach (var EventLink in EventLinks)
		{
			AllowedReflection.PopulateEventRouter(EventLink);
		}
	}
}