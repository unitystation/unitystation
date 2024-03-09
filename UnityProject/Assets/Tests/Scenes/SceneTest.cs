using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace Tests.Scenes
{
	public record SceneTestData(string File)
	{
		// This allows the tests to show the name of the scene rather than the file location
		public override string ToString() => Path.GetFileNameWithoutExtension(File);
	}
	
	public abstract class SceneTest
	{
		public static IEnumerable<SceneTestData> Scenes => Utils.NonDevScenes.Select(scene => new SceneTestData(scene));

		private List<GameObject> rootObjects;

		private SceneTestData Data { get; }

		protected Scene Scene { get; private set; }

		protected TestReport Report { get; private set; }

		protected IReadOnlyList<GameObject> RootObjects => rootObjects;

		protected SceneTest(SceneTestData data) => Data = data;

		public void Setup()
		{
			Scene = EditorSceneManager.OpenScene(Data.File);
			var objectsList = ListPool<GameObject>.Get();
			Scene.GetRootGameObjects(objectsList);
			rootObjects = objectsList;
		}

		public void TearDown()
		{
			ListPool<GameObject>.Release(rootObjects);
			rootObjects = null;
		}

		public void SetupReport() => Report = new TestReport();
	}
}