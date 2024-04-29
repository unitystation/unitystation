using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Logs;
using Mirror;
using SecureStuff;
using Systems.Scenes;
using Random = UnityEngine.Random;

public class TestVariableViewerScript : NetworkBehaviour
{
	public List<int> EmptyPListInt = new List<int>();
	public List<int> PListInt = new List<int>();
	public SyncList<bool> SyncPListbool = new SyncList<bool>();
	public SyncList<string> SyncPListstring = new SyncList<string>();
	public SyncList<Teststruct> SyncPListTeststruct = new SyncList<Teststruct>();
	public SyncList<Connection> SyncPListConnection = new SyncList<Connection>();
	public List<MaintObject> MaintObjectList = new List<MaintObject>();


	private void DOThingPrivate()
	{
		Loggy.Log("DOThingPrivate");
	}


	public void DOThingPublic()
	{
		Loggy.Log("DOThingPublic");
	}


	void Start()
	{
		for (int i = 0; i < 10; i++)
		{
			PListInt.Add(i);
			SyncPListbool.Add(true);
			SyncPListstring.Add(i.ToString() + "< t");
			SyncPListConnection.Add(Connection.East);
			var GG = new Teststruct
			{
				author = ("BOB" + i),
				price = i,
				title = i + "COOL?"
			};
			SyncPListTeststruct.Add(GG);

			// var oo = new MaintObject();
			// oo.ObjectToSpawn = null;
			// oo.ObjectChance = i;
			// oo.RequiredWalls = 2;
			// MaintObjectList.Add(oo);
		}

		netIdentity.isDirty = true;
	}
}


public struct Teststruct
{
	public decimal price;
	public string title;
	public string author;
}