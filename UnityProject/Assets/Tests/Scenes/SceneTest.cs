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

	[Ignore("For scene testing subclasses")]
	[Category(nameof(Scenes))]
	[TestFixtureSource(typeof(SceneTest), nameof(Scenes))]
	public abstract class SceneTest
	{
		public static IEnumerable<SceneTestData> Scenes => Utils.NonDevScenes.Select(scene => new SceneTestData(scene));

		private List<GameObject> rootObjects;

		private SceneTestData Data { get; }

		protected Scene Scene { get; private set; }

		protected TestReport Report { get; private set; }

		protected IReadOnlyList<GameObject> RootObjects => rootObjects;

		protected SceneTest(SceneTestData data) => Data = data;

		[OneTimeSetUp]
		public void Setup()
		{
			Scene = EditorSceneManager.OpenScene(Data.File);
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