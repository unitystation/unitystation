using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScriptableObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CreateAssetMenu(fileName = "TestSingleton", menuName = "Singleton/TestSingleton")]
public class TestSingleton : SingletonScriptableObject<TestSingleton>
{

	public List<TestRunSO> Tests = new List<TestRunSO>();

	public void RunTests()
	{
		foreach (var Test in Tests)
		{
			var report = new StringBuilder();
			var Openedscene = EditorSceneManager.OpenScene("Assets/Scenes/DevScenes/RRT CleanStation.unity");
			//Test.RunTest();
		}
	}

}
