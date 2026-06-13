using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using US13.Items.Bureaucracy;
using US13.Objects.Other;
using US13.Objects.Traps;
using US13.Tilemaps.Behaviours.Layers;
using MapSaverClass = US13.MapSaver.MapSaver;

namespace Tests.MapSerializer
{
	[TestFixture]
	[Category(nameof(Scenes))]
	public class MapSaveLoadBugTests
	{
		private const string TestRoomPath = "TestRooms/MapSerializerTest.json";

		private const string SwitchPrefab = "Assets/Prefabs/Objects/Traps/_GenericWallSwitchBase.prefab";
		private const string LogicGatePrefab = "Assets/Prefabs/Objects/Traps/LogicGateAND.prefab";
		private const string SimpleBookPrefab = "Assets/Prefabs/Items/Bureaucracy/_Base_SimpleBook.prefab";
		private const string TurretPrefab = "Assets/Prefabs/Objects/Security/TurretBase.prefab";

		private List<GameObject> spawnedObjects;

		[SetUp]
		public void Setup()
		{
			MapSerializerTestUtils.OpenEmptyMap();
			spawnedObjects = new List<GameObject>();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var go in spawnedObjects)
			{
				if (go != null) Object.DestroyImmediate(go);
			}

			spawnedObjects.Clear();
			MapSaverClass.CodeClass.ThisCodeClass.Reset();
		}

		[Test]
		public void Smoke_LoadTestRoom_SavesWithoutError()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);
			var json = MapSerializerTestUtils.SaveToJson(matrices);
			Assert.IsNotEmpty(json);
		}

		[Test]
		public void TriggerList_InSceneGameObjectReference_SurvivedRoundTrip()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);
			var parent = matrices[0].ObjectLayer.transform;

			var switchGo = MapSerializerTestUtils.SpawnPrefab(SwitchPrefab, parent);
			var gateGo = MapSerializerTestUtils.SpawnPrefab(LogicGatePrefab, parent);
			spawnedObjects.Add(switchGo);
			spawnedObjects.Add(gateGo);

			var so = new SerializedObject(switchGo.GetComponent<GenericTriggerOutput>());
			so.FindProperty("genericTriggerObjects").ClearArray();
			so.FindProperty("genericTriggerObjects").InsertArrayElementAtIndex(0);
			so.FindProperty("genericTriggerObjects").GetArrayElementAtIndex(0).objectReferenceValue = gateGo;
			so.ApplyModifiedPropertiesWithoutUndo();

			var json = MapSerializerTestUtils.SaveToJson(matrices);
			var saved = JObject.Parse(json);

			var fieldData = saved.SelectTokens("$..ClassDatas[*].Data[*]")
				.FirstOrDefault(t => t["Name"]?.Value<string>() == "genericTriggerObjects#0");

			Assert.IsNotNull(fieldData, "genericTriggerObjects#0 not found in saved JSON");
			Assert.IsNotEmpty(fieldData["Data"]?.Value<string>(), "genericTriggerObjects#0 Data was empty — in-scene reference was dropped");

			MapSerializerTestUtils.OpenEmptyMap();
			MapSerializerTestUtils.LoadIntoFreshMatrix(MapSerializerTestUtils.DeserializeJson(json));

			var loadedSwitch = Object.FindObjectsByType<GenericTriggerOutput>(FindObjectsSortMode.None).FirstOrDefault();
			Assert.IsNotNull(loadedSwitch, "Switch not found after reload");
			var loadedSo = new SerializedObject(loadedSwitch);
			var triggerProp = loadedSo.FindProperty("genericTriggerObjects");
			Assert.AreEqual(1, triggerProp.arraySize, "genericTriggerObjects list empty after reload");
			Assert.IsNotNull(triggerProp.GetArrayElementAtIndex(0).objectReferenceValue, "genericTriggerObjects[0] is null after reload — load-side fix failed");
		}

		[Test]
		public void SimpleBook_ModifiedRemarks_SurvivedRoundTrip()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);
			var parent = matrices[0].ObjectLayer.transform;

			var bookGo = MapSerializerTestUtils.SpawnPrefab(SimpleBookPrefab, parent);
			spawnedObjects.Add(bookGo);

			var so = new SerializedObject(bookGo.GetComponent<SimpleBook>());
			so.FindProperty("remarks").ClearArray();
			so.FindProperty("remarks").InsertArrayElementAtIndex(0);
			so.FindProperty("remarks").GetArrayElementAtIndex(0).stringValue = "test remark unique string";
			so.ApplyModifiedPropertiesWithoutUndo();

			var json = MapSerializerTestUtils.SaveToJson(matrices);
			Assert.IsTrue(json.Contains("test remark unique string"), "Modified remarks not found in saved JSON — field reset to prefab default");

			MapSerializerTestUtils.OpenEmptyMap();
			MapSerializerTestUtils.LoadIntoFreshMatrix(MapSerializerTestUtils.DeserializeJson(json));

			var loadedBook = Object.FindObjectsByType<SimpleBook>(FindObjectsSortMode.None)
				.FirstOrDefault(b =>
				{
					var s = new SerializedObject(b);
					var p = s.FindProperty("remarks");
					return p.arraySize > 0 && p.GetArrayElementAtIndex(0).stringValue == "test remark unique string";
				});
			Assert.IsNotNull(loadedBook, "SimpleBook with modified remarks not found after reload — load-side fix failed");
		}

		[Test]
		public void Turret_SpawnGunSetToNull_SurvivedRoundTrip()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);
			var parent = matrices[0].ObjectLayer.transform;

			var turretGo = MapSerializerTestUtils.SpawnPrefab(TurretPrefab, parent);
			spawnedObjects.Add(turretGo);

			var so = new SerializedObject(turretGo.GetComponent<Turret>());
			so.FindProperty("spawnGun").objectReferenceValue = null;
			so.ApplyModifiedPropertiesWithoutUndo();

			var json = MapSerializerTestUtils.SaveToJson(matrices);
			var saved = JObject.Parse(json);

			var fieldData = saved.SelectTokens("$..ClassDatas[*].Data[*]")
				.FirstOrDefault(t => t["Name"]?.Value<string>() == "spawnGun");

			Assert.IsNotNull(fieldData, "spawnGun field not found in saved JSON — null override was not recorded");
			Assert.AreEqual("NULL", fieldData["Data"]?.Value<string>(), "spawnGun was not saved as NULL — may be reset to default on load");

			MapSerializerTestUtils.OpenEmptyMap();
			MapSerializerTestUtils.LoadIntoFreshMatrix(MapSerializerTestUtils.DeserializeJson(json));

			var loadedTurret = Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).FirstOrDefault();
			Assert.IsNotNull(loadedTurret, "Turret not found after reload");
			var loadedSo = new SerializedObject(loadedTurret);
			Assert.IsNull(loadedSo.FindProperty("spawnGun").objectReferenceValue, "spawnGun was not null after reload — null override was lost");
		}

		[Test]
		public void ChildObject_AddedToPrefabInstance_DataSurvivedRoundTrip()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);
			var parent = matrices[0].ObjectLayer.transform;

			var parentGo = MapSerializerTestUtils.SpawnPrefab(SimpleBookPrefab, parent);
			spawnedObjects.Add(parentGo);

			var child = new GameObject("TestChild");
			child.transform.SetParent(parentGo.transform);
			var book = child.AddComponent<SimpleBook>();
			var so = new SerializedObject(book);
			so.FindProperty("remarks").ClearArray();
			so.FindProperty("remarks").InsertArrayElementAtIndex(0);
			so.FindProperty("remarks").GetArrayElementAtIndex(0).stringValue = "child remark unique string";
			so.ApplyModifiedPropertiesWithoutUndo();

			var json = MapSerializerTestUtils.SaveToJson(matrices);
			Assert.IsTrue(json.Contains("child remark unique string"), "Child object data not found in saved JSON — child data was dropped");

			MapSerializerTestUtils.OpenEmptyMap();
			MapSerializerTestUtils.LoadIntoFreshMatrix(MapSerializerTestUtils.DeserializeJson(json));

			var loadedBook = Object.FindObjectsByType<SimpleBook>(FindObjectsSortMode.None)
				.FirstOrDefault(b =>
				{
					var s = new SerializedObject(b);
					var p = s.FindProperty("remarks");
					return p.arraySize > 0 && p.GetArrayElementAtIndex(0).stringValue == "child remark unique string";
				});
			Assert.IsNotNull(loadedBook, "Child object with data not found after reload — load-side fix failed");
		}
	}
}
