using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecureStuff;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using US13.MapSaver;

namespace Tests.Scenes
{
	public enum BluePrintType
	{
		Map,
		Room
	}

	public record SceneTestData(string File, BluePrintType Type)
	{
		// This allows the tests to show the name of the scene rather than the file location
		public override string ToString() => Path.GetFileNameWithoutExtension(File);
		public string File { get; } = File;

		public BluePrintType Type { get; } = Type;
	}

	[Ignore("For scene testing subclasses")]
	[Category(nameof(Scenes))]
	[TestFixtureSource(typeof(SceneTest), nameof(Scenes))]
	public abstract class SceneTest
	{
		public static IEnumerable<SceneTestData> Scenes => Utils.NonDevScenes.Select(scene => new SceneTestData(scene, BluePrintType.Map))
													.Concat(Utils.RoomBlueprintScenes.Select(scene => new SceneTestData(scene, BluePrintType.Room)));

		private List<GameObject> rootObjects;

		private SceneTestData Data { get; }

		protected Scene Scene { get; private set; }

		protected TestReport Report { get; private set; }

		protected IReadOnlyList<GameObject> RootObjects => rootObjects;

		protected SceneTest(SceneTestData data) => Data = data;

		[OneTimeSetUp]
		public void Setup()
		{

			if (Data.File.Contains("json"))
			{
				Scene = EditorSceneManager.OpenScene("Assets/Scenes/DevScenes/EmptyMap.unity");
				MapSaver.CodeClass.ThisCodeClass.Reset();
				MapSaver.MapData mapData;
				if(Data.Type == BluePrintType.Map) mapData = JsonConvert.DeserializeObject<MapSaver.MapData>(AccessFile.Load(Data.File, FolderType.Maps));
				else mapData = JsonConvert.DeserializeObject<MapSaver.MapData>(AccessFile.Load(Data.File, FolderType.Rooms));
				//TODO: Rooms don't require all the same tests as full maps. Figure out which ones are redundant and skip them. That being said, rooms are only really 1s each
				//TODO: Consider Loading all rooms at once and doing one large check

				List<IEnumerator> PreviousLevels = new List<IEnumerator>();
				var Imnum = MapLoader.ServerLoadMap(Vector3.zero, Vector3.zero, mapData, TestLoad: true);
				bool Loop = true;
				while (Loop && PreviousLevels.Count == 0)
				{
					if ( Imnum.Current is IEnumerator)
					{
						PreviousLevels.Add(Imnum);
						Imnum = (IEnumerator) Imnum.Current;
					}

					Loop = Imnum.MoveNext();
					if (Loop == false)
					{
						if (PreviousLevels.Count > 0)
						{
							Imnum = PreviousLevels[PreviousLevels.Count - 1];
							PreviousLevels.RemoveAt(PreviousLevels.Count - 1);
							Loop = Imnum.MoveNext();
						}
					}
				}
			}
			else
			{
				Scene = EditorSceneManager.OpenScene(Data.File);
			}

			var objectsList = ListPool<GameObject>.Get();
			Scene.GetRootGameObjects(objectsList);
			rootObjects = objectsList;
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			ListPool<GameObject>.Release(rootObjects);
			rootObjects = null;
		}

		[SetUp]
		public void SetupReport() => Report = new TestReport();
	}
}