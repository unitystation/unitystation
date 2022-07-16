using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ScriptableObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameRunTests
{
	[CreateAssetMenu(fileName = "TestSingleton", menuName = "Singleton/TestSingleton")]
	public class TestSingleton : ScriptableObject
	{
		public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
		{
			List<T> assets = new List<T>();
			string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}

			return assets;
		}


		static TestSingleton _instance = null;


		public static TestSingleton Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindAssetsByType<TestSingleton>().FirstOrDefault();
				}

				return _instance;
			}
		}


		public List<TestRunSO> Tests = new List<TestRunSO>();


		[NonSerialized]
		public Dictionary<TestRunSO, Tuple<bool, StringBuilder>> Results =
			new Dictionary<TestRunSO, Tuple<bool, StringBuilder>>();

		public IEnumerator RunTests()
		{
			Results.Clear();

			TestRunSO OverrideTestRunSO = null;

			foreach (var Test in Tests)
			{
				if (Test.RunThisone)
				{
					OverrideTestRunSO = Test;
				}
			}

			foreach (var Test in Tests)
			{
				while (PlayerManager.LocalPlayerObject == null)
				{
					yield return null;
				}

				if (OverrideTestRunSO != null)
				{
					yield return OverrideTestRunSO.RunTest(this);
					break;
				}

				yield return Test.RunTest(this);
				if (Test.DebugThis)
				{
					yield return WaitFor.Seconds(30);
				}

				yield return null;
				GameRunTests.RunRestartRound();
			}

			var Stringbuilder = new StringBuilder();
			bool Fail = false;
			foreach (var Result in Results)
			{
				if (Result.Value.Item1)
				{
					Fail = true;
					Stringbuilder.AppendLine($"################ {Result.Key.name} Failed tests ###################");
					Stringbuilder.Append(Result.Value.Item2);
				}
			}

			if (Fail)
			{
				Assert.Fail(Stringbuilder.ToString());
			}
		}
	}
}