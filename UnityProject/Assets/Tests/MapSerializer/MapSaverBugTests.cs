using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using US13.Items.Bureaucracy;
using US13.Objects.Other;
using US13.Objects.Traps;
using MapSaverClass = US13.MapSaver.MapSaver;

namespace Tests.MapSerializer
{
	[TestFixture]
	[Category(nameof(Scenes))]
	public class MapSaverBugTests
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
		public void TriggerList_InSceneGameObjectReference_SurvivedInJson()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);

			var switchGo = MapSerializerTestUtils.SpawnPrefab(SwitchPrefab);
			var gateGo = MapSerializerTestUtils.SpawnPrefab(LogicGatePrefab);
			spawnedObjects.Add(switchGo);
			spawnedObjects.Add(gateGo);

			var triggerOutput = switchGo.GetComponent<GenericTriggerOutput>();
			triggerOutput.AddTrigger(gateGo.GetComponent<IGenericTrigger>());

			var json = MapSerializerTestUtils.SaveToJson(matrices);
			var saved = JObject.Parse(json);

			var fieldData = saved.SelectTokens("$..ClassDatas[*].Data[*]")
				.FirstOrDefault(t => t["Name"]?.Value<string>() == "genericTriggerObjects#0");

			Assert.IsNotNull(fieldData, "genericTriggerObjects#0 not found in saved JSON");
			Assert.IsNotEmpty(fieldData["Data"]?.Value<string>(), "genericTriggerObjects#0 Data was empty — in-scene reference was dropped");
		}

		[Test]
		public void SimpleBook_ModifiedRemarks_SurvivedInJson()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);

			var bookGo = MapSerializerTestUtils.SpawnPrefab(SimpleBookPrefab);
			spawnedObjects.Add(bookGo);

			var so = new SerializedObject(bookGo.GetComponent<SimpleBook>());
			so.FindProperty("remarks").ClearArray();
			so.FindProperty("remarks").InsertArrayElementAtIndex(0);
			so.FindProperty("remarks").GetArrayElementAtIndex(0).stringValue = "test remark unique string";
			so.ApplyModifiedPropertiesWithoutUndo();

			var json = MapSerializerTestUtils.SaveToJson(matrices);

			Assert.IsTrue(json.Contains("test remark unique string"), "Modified remarks not found in saved JSON — field reset to prefab default");
		}

		[Test]
		public void Turret_SpawnGunSetToNull_SurvivedInJson()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);

			var turretGo = MapSerializerTestUtils.SpawnPrefab(TurretPrefab);
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
		}

		[Test]
		public void ChildObject_AddedToPrefabInstance_DataSurvivedInJson()
		{
			var mapData = MapSerializerTestUtils.DeserializeRoom(TestRoomPath);
			var matrices = MapSerializerTestUtils.LoadIntoFreshMatrix(mapData);

			var parentGo = MapSerializerTestUtils.SpawnPrefab(SimpleBookPrefab);
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
		}
	}
}
