using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Logs;
using Mirror;
using SecureStuff;
using Systems.Scenes;
using Tiles;
using Random = UnityEngine.Random;

public class TestVariableViewerScript : NetworkBehaviour
{

	public List<int> IntList = new List<int>();

	public List<Component> ComponentList = new List<Component>();

	public List<GameObject> GameObjectList = new List<GameObject>();

	public List<LayerTile> Tileslist = new List<LayerTile>();


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
			// PListInt.Add(i);
			// SyncPListbool.Add(true);
			// SyncPListstring.Add(i.ToString() + "< t");
			// SyncPListConnection.Add(Connection.East);
			// var GG = new Teststruct
			// {
			// 	author = ("BOB" + i),
			// 	price = i,
			// 	title = i + "COOL?"
			// };
			// SyncPListTeststruct.Add(GG);

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