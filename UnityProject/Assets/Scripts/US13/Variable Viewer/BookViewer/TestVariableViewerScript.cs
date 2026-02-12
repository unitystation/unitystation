using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Tilemaps.Tiles;

namespace US13.Variable_Viewer.BookViewer
{
	public class TestVariableViewerScript : NetworkBehaviour
	{

		public int BasicINT = 99;

		public List<TestClass> TestClasss = new List<TestClass>();


		public List<int> BasicListUntouched = new List<int>();
		public List<int> BasicListRemoved = new List<int>();
		public List<int> BasicListAdded = new List<int>();

		public List<GameObject> PrefabGameObjectListAdded = new List<GameObject>();

		public List<Component> PrefabComponentListAdded = new List<Component>();

		public List<Component> GameComponentListAdded = new List<Component>();

		public List<LayerTile> SoListAdded = new List<LayerTile>();

		public SerializableDictionary<int, int> BasicDictionary = new SerializableDictionary<int, int>();

		public SerializableDictionary<LayerTile, int> KeySOBasicDictionary = new SerializableDictionary<LayerTile, int>();

		public SerializableDictionary<int, LayerTile> ValSOBasicDictionary = new SerializableDictionary<int, LayerTile>();

		public List<List<int>> BasicListWithinList = new List<List<int>>();

		void Start()
		{
			if (BasicListRemoved.Count > 0)
			{
				BasicListRemoved.RemoveAt(0);
			}


			for (int i = 0; i < 2; i++)
			{
				BasicListAdded.Add(i);
				BasicDictionary[i+3] = i;
				TestClasss.Add(new TestClass()
				{
					price = 324342 + i,
					title = "bob" + i,
					author = "bool le cool"
				});
			}

			if (netIdentity != null)
			{
				netIdentity.isDirty = true;
			}
		}
	}


	public struct Teststruct
	{
		public decimal price;
		public string title;
		public string author;
	}

	[System.Serializable]
	public class TestClass
	{
		public float price;
		public string title;
		public string author;
	}
}