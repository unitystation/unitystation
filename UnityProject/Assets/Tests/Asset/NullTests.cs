using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Core.Editor.Attributes;
using Items;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = System.Object;

namespace Tests.Asset
{
	public class NullTests
	{
		private static string sceneName = "";

		private static State state;

		#region GameObject Checking

		/// <summary>
		/// Checks to make sure all objects in the prefabs with fields with CannotBeNull are not null
		/// </summary>
		[Test]
		public void CheckCannotBeNullPrefab()
		{
			var report = new StringBuilder();
			var prefabGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
			var prefabPaths = prefabGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			state = State.Prefab;

			foreach (var prefab in prefabPaths)
			{
				var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);

				if(gameObject == null) continue;

				CheckCannotBeNullGameObject(gameObject, report);

				RandomUtils.IterateChildren(gameObject,
					delegate(GameObject go) { CheckCannotBeNullGameObject(go, report); },
					true);
			}

			Assert.IsEmpty(report.ToString());
		}

		/// <summary>
		/// Checks to make sure all objects in the scenes with fields with CannotBeNull are not null
		/// </summary>
		[Test]
		public void CheckCannotBeNullScene()
		{
			var report = new StringBuilder();
			var scenesGUIDs = AssetDatabase.FindAssets("t:Scene", new string[] {"Assets/Scenes"});
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			state = State.Scene;

			foreach (var scene in scenesPaths)
			{
				if (scene.Contains("DevScenes") || scene.StartsWith("Packages")) continue;

				var openScene = EditorSceneManager.OpenScene(scene);

				sceneName = openScene.name;

				var gameObjects = openScene.GetRootGameObjects();

				foreach (var gameObject in gameObjects)
				{
					RandomUtils.IterateChildren(gameObject,
						delegate(GameObject go) { CheckCannotBeNullGameObject(go, report); },
						true);
				}
			}

			Assert.IsEmpty(report.ToString());
		}

		private void CheckCannotBeNullGameObject(GameObject toCheck, StringBuilder report)
		{
			var components = toCheck.GetComponents<MonoBehaviour>();

			foreach (var component in components)
			{
				CheckTypes(component, report, component.GetType());
			}
		}

		#endregion

		#region Scriptable Object Checking

		/// <summary>
		/// Checks to make sure all ScriptableObjects with fields with CannotBeNull are not null
		/// </summary>
		[Test]
		public void CheckCannotBeNullScriptableObject()
		{
			var report = new StringBuilder();
			var prefabGUIDs = AssetDatabase.FindAssets("t:ScriptableObject", new string[] {"Assets"});
			var prefabPaths = prefabGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			state = State.ScriptableObject;

			foreach (var prefab in prefabPaths)
			{
				var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(prefab);

				if(scriptableObject == null) continue;

				CheckTypes(scriptableObject, report, scriptableObject.GetType());
			}

			Assert.IsEmpty(report.ToString());
		}

		#endregion

		private void CheckTypes<T>(T toCheck, StringBuilder report, Type type, int count = 0) where T : UnityEngine.Object
		{
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var field in fields)
			{
				CheckField(toCheck, report, field);

				CheckList(toCheck, report, field);
			}

			count++;

			if(count > 3) return;

			foreach (var field in fields)
			{
				CheckTypes(toCheck, report, field.GetType(), count);
			}
		}

		private void CheckField<T>(T toCheck, StringBuilder report, FieldInfo checkField) where T : UnityEngine.Object
		{
			if (Attribute.IsDefined(checkField, typeof(CannotBeNullAttribute)) == false) return;

			if(checkField.GetValue(toCheck) != null) return;

			if (state == State.Prefab)
			{
				report.AppendLine($"{toCheck.name} prefab has a null value on {checkField.GetType().Name}");
			}
			else if (state == State.Scene)
			{
				report.AppendLine(
					$"{toCheck.name} scene object has a null value on {checkField.GetType().Name} in scene: {sceneName}");
			}
			else
			{
				report.AppendLine($"{toCheck.name} scriptable object has a null value on field: {checkField.Name}");
			}
		}

		private void CheckList<T>(T toCheck, StringBuilder report, FieldInfo checkField) where T : UnityEngine.Object
		{
			Type type = checkField.GetType();

			if(type.IsGenericType)
			{
				//TODO this isnt detecting lists???

				Debug.LogError($"{type.Name} is generic");
				if (type.GetGenericTypeDefinition()
				    != typeof(List<>))
				{
					Debug.LogError($"{type.Name} is not list");
					return;
				}

				Type itemType = type.GetGenericArguments()[0];

				Debug.LogError($"{type.Name} {itemType.Name}");

				//TODO need to check instance first?

				CheckTypes(toCheck, report, itemType);
			}
		}

		private enum State
		{
			Prefab,
			Scene,
			ScriptableObject
		}
	}
}
