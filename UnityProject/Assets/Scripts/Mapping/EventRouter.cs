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
	//why Singular, gets complicated if you add multiple since you have to do a Equality of the elements of the list
	public EventConnection Connection1 = new EventConnection();
	public EventConnection Connection2 = new EventConnection();
	public EventConnection Connection3 = new EventConnection();
	public EventConnection Connection4 = new EventConnection();
	public EventConnection Connection5 = new EventConnection();


	// Start is called before the first frame update
	void Start()
	{
		AllowedReflection.PopulateEventRouter(Connection1);
		AllowedReflection.PopulateEventRouter(Connection2);
		AllowedReflection.PopulateEventRouter(Connection3);
		AllowedReflection.PopulateEventRouter(Connection4);
		AllowedReflection.PopulateEventRouter(Connection5);
	}
}