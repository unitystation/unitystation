using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logs;
using SecureStuff;
using UnityEngine;
using System.Linq;
using System.Runtime.Serialization;
using Mirror;
using Newtonsoft.Json;
using NUnit.Compatibility;
using UnityEngine.Events;

namespace SecureStuff
{
	public interface IPopulateIDRelation
	{
		public void PopulateIDRelation(HashSet<FieldData> FieldDatas, FieldData fieldData, Component mono,
			bool UseInstance = false);

		public void FlagSaveKey(string RootID, Component Object, FieldData FieldData);

		public object ObjectsFromForeverID(string ForeverID, Type InType);
		public Dictionary<string, GameObject> Objects { get; } //Look up dictionaries
	}


	public class SceneObjectReference : BaseAttribute
	{
	}

	public struct PrefabComponent
	{
		public string ForeverId;
		public string ComponentName;
	}

	public class UnprocessedData
	{
		public Component Object;
		public FieldData FieldData;
		public string ID;
	}

	public class FieldData
	{
		private List<string> ReferencingIDs;

		private List<Component> RuntimeReferences;

		public virtual List<Component> GetRuntimeReferences()
		{
			return RuntimeReferences;
		}

		public virtual void RemoveRuntimeReference(Component inRuntimeReference)
		{
			if (RuntimeReferences == null)
			{
				return;
			}

			RuntimeReferences.Remove(inRuntimeReference);
		}

		public virtual void AddRuntimeReference(Component inRuntimeReference)
		{
			if (RuntimeReferences == null)
			{
				RuntimeReferences = new List<Component>();
			}

			RuntimeReferences.Add(inRuntimeReference);
		}

		public virtual void AddID(string ToAdd)
		{
			if (ReferencingIDs == null)
			{
				ReferencingIDs = new List<string>();
			}

			ReferencingIDs.Add(ToAdd);
		}

		public virtual void Serialise()
		{
			if (ReferencingIDs == null) return;
			bool First = true;
			foreach (var ID in ReferencingIDs)
			{
				if (First)
				{
					Data = Data + ID;
				}
				else
				{
					Data = Data + "," + ID;
				}

				First = false;
			}
		}

		public string Name;
		public string Data;
		public bool? IsPrefabID;
	}

	public static class SecureMapsSaver
	{
		private static void ListHandleLoad(string RootID, Component Root, FieldInfo Field, object Object,
			FieldData ModField, int Index,
			IPopulateIDRelation IPopulateIDRelation, bool IsServer = true, string AdditionalJumps = "")
		{
			var List = (Field.GetValue(Object) as IList);

			if (ModField.Data is "#removed#")
			{
				List.Remove(Index);
				return;
			}

			if (ModField.Data is "NULL")
			{
				List[Index] = null; // never have to worry about value type because It can never be null on the map to
				return;
			}
			var ListType = Field.FieldType.GetGenericArguments()[0];

			bool ScriptObject = typeof(ScriptableObject).IsAssignableFrom(ListType) &&
			          typeof(IHaveForeverID).IsAssignableFrom(ListType);

			bool GameObject = typeof(GameObject).IsAssignableFrom(ListType);

			bool Component = typeof(Component).IsAssignableFrom(ListType);

			bool IsClass = ListType.IsValueType == false
			               && (ListType == typeof(string)) == false
			               && ListType.IsGenericType == false
			               && ListType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0;


			while (List.Count <= Index)
				//TODO Could be exploited? well You could just have a map with a million objects so idk xD
			{
				if ((GameObject == false && Component == false && ScriptObject == false && IsClass)
					|| ListType.IsValueType)
				{
					//NOTEE is dangerous
					List.Add(Activator.CreateInstance(ListType));
				}
				else
				{
					List.Add(null);
				}
			}

			if (GameObject)
			{
				if (ModField.IsPrefabID == true)
				{
					var Prefab = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, ListType);
					List[Index] = ((GameObject) Prefab);
				}
				else
				{
					Loggy.LogError("Needs to be added!!!");
					//TODO Implement!!
				}
			}
			else if (Component)
			{
				if (ModField.IsPrefabID == true)
				{
					var PrefabComponent = JsonConvert.DeserializeObject<PrefabComponent>(ModField.Data);
					var Prefab = IPopulateIDRelation.ObjectsFromForeverID(PrefabComponent.ForeverId, ListType);
					List[Index] = ((GameObject) Prefab).GetComponent(PrefabComponent.ComponentName);
				}
				else
				{
					var obs = GetComponentPath(ModField.Data, IPopulateIDRelation, out bool AllLoaded);

					if (AllLoaded == false)
					{
						IPopulateIDRelation.FlagSaveKey(RootID, Root, ModField);
						return;
					}

					List[Index] = obs;
				}
			}
			else if (ScriptObject)
			{
				var SO = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, ListType);
				List[Index] = SO;
			}
			else
			{
				if (IsClass)
				{
					ProcessIndividualField(RootID, Root, List[Index], ModField, IPopulateIDRelation, AdditionalJumps,
						IsServer);
				}
				else
				{
					List[Index] = Librarian.Page.DeSerialiseValue(ModField.Data, ListType);
				}
			}
		}


		private static void DictionaryHandleLoad(string RootID, Component Root, FieldInfo Field, object Object,
			FieldData ModField, string Index,
			IPopulateIDRelation IPopulateIDRelation, string AdditionalJumps, bool IsServer = true)
		{
			bool KeyisScriptableObject = false;
			bool KeyIsComponent = false;

			bool KeyanyOfThem = false;

			var Arguments = Field.FieldType.GetGenericArguments();

			if (Arguments.Length == 0)
			{
				Arguments = GetGenericBaseTypeArgument(Field.FieldType);
			}

			var KeyType = Arguments[0];

			if (typeof(GameObject).IsAssignableFrom(KeyType))
			{
				KeyIsComponent = false;
				KeyisScriptableObject = false;
				KeyanyOfThem = true;
			}
			else if (typeof(Component).IsAssignableFrom(KeyType))
			{
				KeyIsComponent = true;
				KeyisScriptableObject = false;
				KeyanyOfThem = true;
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(KeyType) &&
			         typeof(IHaveForeverID).IsAssignableFrom(KeyType))
			{
				KeyIsComponent = false;
				KeyisScriptableObject = true;
				KeyanyOfThem = true;
			}


			if (typeof(System.Action).IsAssignableFrom(KeyType)
			    || (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(KeyType))
			    || (KeyType.IsGenericType && KeyType.GetGenericTypeDefinition() == typeof(Action<>)))
			{
				//Actions can get confused with runtime added onces vs Mapped Ones
				return;
			}

			bool KeyIsClass = KeyanyOfThem == false
			                  && KeyType.IsValueType == false
			                  && (KeyType == typeof(string)) == false
			                  && KeyType.IsGenericType == false
			                  && KeyType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0;

			if (KeyIsClass == true) return; //is not Supported

			if (KeyanyOfThem == false && KeyIsClass == false)
			{
				if (KeyType.IsValueType == false)
				{
					return; //Non-serialisable class
				}
			}


			bool ValisScriptableObject = false;
			bool ValIsComponent = false;

			bool ValanyOfThem = false;

			var ValType = Arguments[1];

			if (typeof(GameObject).IsAssignableFrom(ValType))
			{
				ValIsComponent = false;
				ValisScriptableObject = false;
				ValanyOfThem = true;
			}
			else if (typeof(Component).IsAssignableFrom(ValType))
			{
				ValIsComponent = true;
				ValisScriptableObject = false;
				ValanyOfThem = true;
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(ValType) &&
			         typeof(IHaveForeverID).IsAssignableFrom(ValType))
			{
				ValIsComponent = false;
				ValisScriptableObject = true;
				ValanyOfThem = true;
			}

			bool ValIsClass = ValanyOfThem == false
			                  && ValType.IsValueType == false
			                  && (ValType == typeof(string)) == false
			                  && ValType.IsGenericType == false
			                  && ValType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0;

			if (typeof(System.Action).IsAssignableFrom(KeyType)
			    || (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(KeyType))
			    || (KeyType.IsGenericType && KeyType.GetGenericTypeDefinition() == typeof(Action<>)))
			{
				//Actions can get confused with runtime added onces vs Mapped Ones
				return;
			}

			if (ValType.IsGenericType) return; //No list within lists for now
			if (ValanyOfThem == false && ValIsClass == false)
			{
				if (ValType.IsValueType == false)
				{
					return; //Non-serialisable class
				}
			}


			var Dictionary = (Field.GetValue(Object) as IDictionary);


			object Key = null;


			if (KeyIsComponent)
			{
				if (ModField.IsPrefabID == true)
				{
					var Split = Index.Split("#", 2);
					var Prefab = IPopulateIDRelation.ObjectsFromForeverID(Split[0], KeyType);
					Key = ((GameObject) Prefab).GetComponent(Split[1]);
				}
				else
				{
					Key = GetComponentPath(Index, IPopulateIDRelation, out bool loaded);
					if (loaded == false)
					{
						IPopulateIDRelation.FlagSaveKey(RootID, Root, ModField);
						return;
					}
				}
			}
			else if (KeyisScriptableObject)
			{
				Key = IPopulateIDRelation.ObjectsFromForeverID(Index, KeyType);
			}
			else if (KeyanyOfThem) //Is game object
			{
				if (ModField.IsPrefabID == true)
				{
					Key = IPopulateIDRelation.ObjectsFromForeverID(Index, KeyType);
				}
				else
				{
					Loggy.LogError("Needs to be added!!!");
					//TODO Implement!!
					return;
				}
			}
			else
			{
				Key = Librarian.Page.DeSerialiseValue(Index, KeyType);
			}


			if (AdditionalJumps == "" && ModField.Data is "#removed#")
			{
				Dictionary.Remove(Key);
				return;
			}


			if (AdditionalJumps == "" && ModField.Data is "NULL")
			{
				Dictionary[Key] = null;
				return;
			}

			if (ValIsComponent)
			{
				if (ModField.IsPrefabID == true)
				{
					var prefabComponent = JsonConvert.DeserializeObject<PrefabComponent>(ModField.Data);
					var prefab = IPopulateIDRelation.ObjectsFromForeverID(prefabComponent.ForeverId, ValType);
					Dictionary[Key] = ((GameObject) prefab).GetComponent(prefabComponent.ComponentName);
				}
				else
				{
					var Value = GetComponentPath(Index, IPopulateIDRelation, out bool loaded);
					if (loaded == false)
					{
						IPopulateIDRelation.FlagSaveKey(RootID, Root, ModField);
						return;
					}
					else
					{
						Dictionary[Key] = Value;
					}
				}
			}
			else if (ValisScriptableObject)
			{
				Dictionary[Key] = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, ValType);
			}
			else if (ValanyOfThem)
			{
				if (ModField.IsPrefabID == true)
				{
					Dictionary[Key] = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, ValType);
				}
				else
				{
					Loggy.LogError("Needs to be added!!!");
					//TODO Implement!!
					return;
				}
			}
			else
			{
				if (ValIsClass)
				{
					if (Dictionary.Contains(Key) == false)
					{
						//NOTEE is dangerous
						Dictionary[Key] = (Activator.CreateInstance(ValType));
					}

					ProcessIndividualField(RootID, Root, Dictionary[Key], ModField, IPopulateIDRelation,
						AdditionalJumps, IsServer);
				}
				else
				{
					Dictionary[Key] = Librarian.Page.DeSerialiseValue(ModField.Data, ValType);
				}
			}
		}

		private static bool ReturnKey(object SpawnedInstance, bool isScriptableObject, bool IsComponent, bool anyOfThem,
			Type Type, out string data)
		{
			if (isScriptableObject)
			{
				data = (SpawnedInstance as IHaveForeverID)?.ForeverID;
				data = data?.Replace("@", "");
				data = data?.Replace("#", "");
				return true;
			}
			else
			{
				if (anyOfThem)
				{
					if (IsComponent)
					{
						var Component = (SpawnedInstance as Component);
						if (Component.transform.parent == null) //Prefab
						{
							var ForeverID = Component.GetComponent<IHaveForeverID>();
							if (ForeverID != null)
							{
								data = ForeverID.ForeverID + "#" + Component.GetType().Name;
								data = data?.Replace("@", "");
								data = data?.Replace("#", "");
								return true;
							}

							data = "";
							return false;
						}
						else
						{
							Loggy.LogError("Not compatible Component");
							//This is due PopulateIDRelation Not supporting it in line of name
							//IPopulateIDRelation.PopulateIDRelation(FieldDatas, FieldData, Component,UseInstance); //Callout
							data = ""; //Not compatible
							return false;
						}
					}
					else
					{
						var GameObjectModified = (SpawnedInstance as GameObject);
						if (GameObjectModified != null && GameObjectModified.transform.parent == null) //Prefab
						{
							var ForeverID = GameObjectModified.GetComponent<IHaveForeverID>();
							if (ForeverID != null)
							{
								data = ForeverID.ForeverID;
								data = data?.Replace("@", "");
								data = data?.Replace("#", "");
								return true;
							}
						}
						else
						{
							data = ""; //Not compatible
							Loggy.LogError("Not compatible GameObject");
							return false;
						}
					}
				}
				else
				{
					data = Librarian.Page.Serialise(SpawnedInstance, Type);
					data = data?.Replace("@", "");
					data = data?.Replace("#", "");
					return true;
				}
			}

			Loggy.LogError("Not compatible HELP");
			data = ""; //Not compatible
			return false;
		}

		// Method to check if a type inherits from any generic class and get the type argument
		private static Type[] GetGenericBaseTypeArgument(Type type)
		{
			while (type != null && type != typeof(object))
			{
				// Check if the type is a generic type
				if (type.IsGenericType)
				{
					// Return the type argument of the generic base class
					return type.GetGenericArguments();
				}

				// Move to the base type
				type = type.BaseType;
			}

			// No generic base class found
			return null;
		}

		private static void DictionaryHandleSave(object MonoSet, object PrefabDefault, FieldInfo Field,
			HashSet<FieldData> FieldDatas, string Prefix, bool UseInstance, IPopulateIDRelation IPopulateIDRelation,
			HashSet<Component> OnGameObjectComponents, HashSet<GameObject> AllGameObjectOnObject)
		{
			bool KeyisScriptableObject = false;
			bool KeyIsComponent = false;

			bool KeyanyOfThem = false;

			var Arguments = Field.FieldType.GetGenericArguments();

			if (Arguments.Length == 0)
			{
				Arguments = GetGenericBaseTypeArgument(Field.FieldType);
			}

			var KeyType = Arguments[0];

			if (typeof(GameObject).IsAssignableFrom(KeyType))
			{
				KeyIsComponent = false;
				KeyisScriptableObject = false;
				KeyanyOfThem = true;
			}
			else if (typeof(Component).IsAssignableFrom(KeyType))
			{
				KeyIsComponent = true;
				KeyisScriptableObject = false;
				KeyanyOfThem = true;
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(KeyType) &&
			         typeof(IHaveForeverID).IsAssignableFrom(KeyType))
			{
				KeyIsComponent = false;
				KeyisScriptableObject = true;
				KeyanyOfThem = true;
			}


			if (typeof(System.Action).IsAssignableFrom(KeyType)
			    || (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(KeyType))
			    || (KeyType.IsGenericType && KeyType.GetGenericTypeDefinition() == typeof(Action<>)))
			{
				//Actions can get confused with runtime added onces vs Mapped Ones
				return;
			}

			bool KeyIsClass = KeyanyOfThem == false
			                  && KeyType.IsValueType == false
			                  && (KeyType == typeof(string)) == false
			                  && KeyType.IsGenericType == false
			                  && KeyType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0;

			if (KeyIsClass == true) return; //is not Supported

			if (KeyanyOfThem == false && KeyIsClass == false)
			{
				if (KeyType.IsValueType == false)
				{
					return; //Non-serialisable class
				}
			}


			bool ValisScriptableObject = false;
			bool ValIsComponent = false;

			bool ValanyOfThem = false;

			var ValType = Arguments[1];

			if (typeof(GameObject).IsAssignableFrom(ValType))
			{
				ValIsComponent = false;
				ValisScriptableObject = false;
				ValanyOfThem = true;
			}
			else if (typeof(Component).IsAssignableFrom(ValType))
			{
				ValIsComponent = true;
				ValisScriptableObject = false;
				ValanyOfThem = true;
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(ValType) &&
			         typeof(IHaveForeverID).IsAssignableFrom(ValType))
			{
				ValIsComponent = false;
				ValisScriptableObject = true;
				ValanyOfThem = true;
			}

			bool ValIsClass = ValanyOfThem == false
			                  && ValType.IsValueType == false
			                  && (ValType == typeof(string)) == false
			                  && ValType.IsGenericType == false
			                  && ValType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0;

			if (typeof(System.Action).IsAssignableFrom(KeyType)
			    || (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(KeyType))
			    || (KeyType.IsGenericType && KeyType.GetGenericTypeDefinition() == typeof(Action<>)))
			{
				//Actions can get confused with runtime added onces vs Mapped Ones
				return;
			}

			if (ValType.IsGenericType) return; //No list within lists for now
			if (ValanyOfThem == false && ValIsClass == false)
			{
				if (ValType.IsValueType == false)
				{
					return; //Non-serialisable class
				}
			}

			var modified = (MonoSet as IDictionary);

			IDictionary original = null;

			if (PrefabDefault != null)
			{
				original = (PrefabDefault as IDictionary);
			}

			foreach (var Key in modified.Keys)
			{
				object ValueOriginal = null;

				bool MarkAsRemoved = false;

				if (original?.Contains(Key) is true) //has key
				{
					ValueOriginal = original[Key];
				}

				if (ReturnKey(Key, KeyisScriptableObject, KeyIsComponent, KeyanyOfThem, KeyType,
					    out var StringData))
				{
					if (ValIsClass && modified[Key] == null)
					{
						RecursiveSearchData(OnGameObjectComponents, AllGameObjectOnObject, IPopulateIDRelation,
							FieldDatas,
							Prefix + Field.Name + "#" + StringData + "@",
							original[Key],
							modified[Key],
							UseInstance); //Recursive
					}
					else
					{
						if (CheckAreSame(ValueOriginal, modified[Key], OnGameObjectComponents, AllGameObjectOnObject) ==
						    false)
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name + "#" + StringData;
							SetDataFieldFor(fieldData, modified[Key], ValisScriptableObject, ValIsComponent,
								ValanyOfThem,
								ValType, FieldDatas, UseInstance, IPopulateIDRelation, false);
							FieldDatas.Add(fieldData); //add data
						}
					}
				}
			}

			if (original != null)
			{
				foreach (var OriginalKey in original.Keys)
				{
					if (modified.Contains(OriginalKey) == false)
					{
						if (ReturnKey(OriginalKey, KeyisScriptableObject, KeyIsComponent, KeyanyOfThem, KeyType,
							    out var StringData))
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name + "#" + StringData;
							SetDataFieldFor(fieldData, null, ValisScriptableObject, ValIsComponent, ValanyOfThem,
								ValType, FieldDatas, UseInstance, IPopulateIDRelation, true);
							FieldDatas.Add(fieldData); //add data
						}
					}
				}
			}
		}


		private static void ListHandleSave(object MonoSet, object PrefabDefault, FieldInfo Field,
			HashSet<FieldData> FieldDatas, string Prefix, bool UseInstance, IPopulateIDRelation IPopulateIDRelation,
			HashSet<Component> OnGameObjectComponents,
			HashSet<GameObject> AllGameObjectOnObject)
		{
			if (MonoSet == null) return;

			bool isScriptableObject = false;
			bool IsComponent = false;

			bool IsReferencedObject = false;

			if (typeof(GameObject).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
			{
				IsComponent = false;
				isScriptableObject = false;
				IsReferencedObject = true;
			}
			else if (typeof(Component).IsAssignableFrom(
				         Field.FieldType.GetGenericArguments()[0]))
			{
				IsComponent = true;
				isScriptableObject = false;
				IsReferencedObject = true;
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(
				         Field.FieldType.GetGenericArguments()[0]) &&
			         typeof(IHaveForeverID).IsAssignableFrom(
				         Field.FieldType.GetGenericArguments()[0]))
			{
				IsComponent = false;
				isScriptableObject = true;
				IsReferencedObject = true;
			}


			var ListType = Field.FieldType.GetGenericArguments()[0];

			bool IsClass = IsReferencedObject == false
			               && ListType.IsValueType == false
			               && (ListType == typeof(string)) == false
			               && ListType.IsGenericType == false
			               && ListType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0;

			if (IsReferencedObject == false && IsClass == false)
			{
				if (ListType.IsValueType == false)
				{
					return; //Non-serialisable class
				}
			}


			if (typeof(System.Action).IsAssignableFrom(ListType)
			    || (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(ListType))
			    || (ListType.IsGenericType && ListType.GetGenericTypeDefinition() == typeof(Action<>)))
			{
				//Actions can get confused with runtime added onces vs Mapped Ones
				return;
			}

			var modified = IEnumeratorToList((MonoSet as IEnumerable).GetEnumerator());

			List<object> original = new List<object>();


			if (PrefabDefault != null)
			{
				original = IEnumeratorToList((PrefabDefault as IEnumerable).GetEnumerator());
			}


			for (int i = 0; i < Math.Max(original.Count, modified.Count); i++)
			{
				if ((i < original.Count && i < modified.Count) || (i < modified.Count && i >= original.Count))
				{
					object OriginalValue = null;
					if (i < original.Count)
					{
						OriginalValue = original[i];
					}

					if (IsClass && modified[i] != null)
					{
						RecursiveSearchData(OnGameObjectComponents, AllGameObjectOnObject,
							IPopulateIDRelation,
							FieldDatas,
							Prefix + Field.Name + "#" + i + "@",
							OriginalValue,
							modified[i],
							UseInstance); //Recursive
					}
					else
					{
						if (CheckAreSame(OriginalValue, modified[i], OnGameObjectComponents,
							    AllGameObjectOnObject) == false)
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name + "#" + i;
							SetDataFieldFor(fieldData, modified[i], isScriptableObject, IsComponent,
								IsReferencedObject,
								ListType, FieldDatas, UseInstance, IPopulateIDRelation, false);
							FieldDatas.Add(fieldData); //add data
						}
					}
				}
				else if (i < original.Count)
				{
					FieldData fieldData = new FieldData();
					fieldData.Name = Prefix + Field.Name + "#" + i;
					SetDataFieldFor(fieldData, null, isScriptableObject, IsComponent, IsReferencedObject,
						ListType, FieldDatas, UseInstance, IPopulateIDRelation, true);
					FieldDatas.Add(fieldData); //add data
				}
			}
		}

		private static GameObject GetGameObjectPath(string Id, IPopulateIDRelation IPopulateIDRelation, out bool Loaded)
		{
			if (Id == "MISSING")
			{
				Loaded = true;
				Loggy.LogError("Map has missing references");
				return null;
			}

			var IDPath = Id.Split("@");
			if (IPopulateIDRelation.Objects.ContainsKey(IDPath[0]) == false)
			{
				Loaded = false;
				return null;
			}
			else
			{
				Loaded = true;
			}

			var Object = IPopulateIDRelation.Objects[IDPath[0]];
			//0,1
			List<int> IDs = new List<int>();

			if (IDPath[1].Contains(
				    ",")) //Technically it always has 0 , but we can ignore it so don't remove it technically
			{
				IDs = IDPath[1].Split().Select(x => int.Parse(x)).ToList();
				IDs.RemoveAt(0);
			}


			while (IDs.Count > 0)
			{
				Object = Object.transform.GetChild(IDs[0]).gameObject;
				IDs.RemoveAt(0);
			}


			return Object;
		}

		private static Component GetComponentPath(string Id, IPopulateIDRelation IPopulateIDRelation, out bool Loaded)
		{
			if (Id == "MISSING")
			{
				Loggy.LogError("Map has missing references");
				Loaded = true;
				return null;
			}

			var IDPath = Id.Split("@");
			var Object = GetGameObjectPath(Id, IPopulateIDRelation, out Loaded);
			if (Loaded == false) return null;
			return Object.GetComponent(IDPath[2]); //TODO Support multiple;
		}

		private static void ProcessIndividualField(string RootID, Component root, object Object,
			FieldData ModField,
			IPopulateIDRelation IPopulateIDRelation,
			string AppropriateName = "", bool IsServer = true)
		{
			var TypeMono = Object.GetType();
			string Index = "";

			bool MoreSteps = false;
			string AdditionalJumps = "";
			if (string.IsNullOrEmpty(AppropriateName))
			{
				AppropriateName = ModField.Name;
			}


			if (AppropriateName.Contains("@"))
			{
				var NewPath = AppropriateName.Split("@", 2);
				AppropriateName = NewPath[0];
				AdditionalJumps = NewPath[1];
			}

			//EventLinks#0#TargetComponent
			if (AppropriateName.Contains("#"))
			{
				var Split = AppropriateName.Split("#", 2);
				AppropriateName = Split[0];
				Index = Split[1];
				if (Index.Contains("@")) //Contains further steps
				{
					Index = Index.Split("@", 2)[0];
				}
			}

			var Field = TypeMono.GetField(AppropriateName,
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy);

			if (IsGoodField(Field) == false) return;

			if (IsServer == false)
			{
				if (IsGoodClientField(Field) == false) return;
			}


			if (Field.FieldType.IsValueType == false &&
			    (Field.FieldType == typeof(string)) == false) //Cross object references
			{
				IEnumerable list = null;

				if ((typeof(IDictionary<,>).IsAssignableFrom(Field.FieldType) ||
				     typeof(IDictionary).IsAssignableFrom(Field.FieldType)))
				{
					//no Field.FieldType.IsGenericType && due to stupid class dictionary inheritance silly unity stuff
					if (typeof(ISerializationCallbackReceiver)
					    .IsAssignableFrom(Field
						    .FieldType)) //so Serialisable dictionary only, can't directly reference due to assembly stuff
					{
						DictionaryHandleLoad(RootID, root, Field, Object, ModField, Index, IPopulateIDRelation,
							AdditionalJumps, IsServer);
					}

					return;
				}


				if (Field.FieldType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(Field.FieldType) &&
				    Field.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
				{
					ListHandleLoad(RootID, root, Field, Object, ModField, int.Parse(Index), IPopulateIDRelation,
						IsServer, AdditionalJumps);
				}

				if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Component)))
				{
					if (IsServer == false)
					{
						if (IsGoodClientField(Field) == false)
						{
							return;
						}
					}

					if (ModField.IsPrefabID == true)
					{
						var PrefabComponent = JsonConvert.DeserializeObject<PrefabComponent>(ModField.Data);
						var Prefab =
							IPopulateIDRelation.ObjectsFromForeverID(PrefabComponent.ForeverId,
								Field.FieldType);

						Field.SetValue(Object,
							((GameObject) Prefab)?.GetComponent(PrefabComponent.ComponentName));
					}
					else
					{
						var data = GetComponentPath(ModField.Data, IPopulateIDRelation, out var AllLoaded);
						if (AllLoaded == false)
						{
							IPopulateIDRelation.FlagSaveKey(RootID, root, ModField);
						}

						Field.SetValue(Object, data);
						return;
					}
				}

				if (Field.FieldType == typeof(UnityEngine.GameObject))
				{
					if (ModField.IsPrefabID == true)
					{
						var Prefab = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, Field.FieldType);
						Field.SetValue(Object, ((GameObject) Prefab));
					}
					else
					{
						var data = GetGameObjectPath(ModField.Data, IPopulateIDRelation, out var AllLoaded);
						if (AllLoaded == false)
						{
							IPopulateIDRelation.FlagSaveKey(RootID, root, ModField);
						}

						Field.SetValue(Object, data);
						return;
					}
				}

				if (Field.FieldType.IsSubclassOf(typeof(ScriptableObject)))
				{
					var SO = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, Field.FieldType);
					Field.SetValue(Object, SO);
					return;
				}

				//if Field is a class and is not related to unity engine.object Serialise it
				if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object))) return;

				if (Field.FieldType.IsGenericType == false)
				{
					ProcessIndividualField(RootID, root, Field.GetValue(Object), ModField, IPopulateIDRelation,
						AdditionalJumps, IsServer);
					return;
				}
			}


			if (Field.FieldType.IsGenericType &&
			    Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return; //skipping all dictionaries For now
			if (Field.FieldType == typeof(System.Action)) return;
			if (Field.FieldType.BaseType == typeof(UnityEngine.Events.UnityEventBase))
				return; //TODO Handle separately Since it is same as Object references

			if (Field.FieldType.IsGenericType) return; //Unity editor can't handle this currently so same Functionality
			Field.SetValue(Object, Librarian.Page.DeSerialiseValue(ModField.Data, Field.FieldType));
		}

		private static void LoadDatarecursive(string RootID, Component root, object Object,
			HashSet<FieldData> IndividualObject,
			IPopulateIDRelation IPopulateIDRelation,
			bool IsServer = true) //Has to be Component to restrict it from being used in silly places
		{
			try
			{
				foreach (var ModField in IndividualObject)
				{
					try
					{
						ProcessIndividualField(RootID, root, Object, ModField, IPopulateIDRelation, "", IsServer);
					}
					catch (Exception e)
					{
						Loggy.LogError(e.ToString());
					}
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}
		}

		public static void LoadData(string RootID, Component Object, HashSet<FieldData> IndividualObject,
			IPopulateIDRelation IPopulateIDRelation,
			bool IsServer = true) //Has to be Component to restrict it from being used in silly places
		{
			if (Object == null) return;
			LoadDatarecursive(RootID, Object, Object, IndividualObject, IPopulateIDRelation, IsServer);
		}

		private static bool IsGoodClientField(FieldInfo Field)
			//So the client doesn't overwrite synchronised values from the server
		{
			var attribute = Field.GetCustomAttributes(typeof(IsSyncedAttribute), true);
			if (attribute.Length > 0)
			{
				return false;
			}

			attribute = Field.GetCustomAttributes(typeof(SyncVarAttribute), true);
			if (attribute.Length > 0)
			{
				return false;
			}

			if (IsSubclassOfRawGeneric(typeof(SyncList<>), Field.FieldType))
			{
				return false;
			}

			if (IsSubclassOfRawGeneric(typeof(SyncDictionary<,>), Field.FieldType))
			{
				return false;
			}

			if (IsSubclassOfRawGeneric(typeof(SyncHashSet<>), Field.FieldType))
			{
				return false;
			}


			if (IsSubclassOfRawGeneric(typeof(SyncSortedSet<>), Field.FieldType))
			{
				return false;
			}

			return true;
		}

		private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
		{
			while (toCheck != null && toCheck != typeof(object))
			{
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (generic == cur)
				{
					return true;
				}

				toCheck = toCheck.BaseType;
			}

			return false;
		}

		private static bool HasAttribute(FieldInfo field, Type attributeType)
		{
			return field.GetCustomAttributes(attributeType, true).Length > 0;
		}

		private static bool IsGoodField(FieldInfo Field)
		{
			if (Field.IsPrivate || Field.IsAssembly || Field.IsFamily)
			{
				if (!HasAttribute(Field, typeof(SerializeField)) ||
				    HasAttribute(Field, typeof(HideInInspector)) ||
				    HasAttribute(Field, typeof(NaughtyAttributes.ReadOnlyAttribute)) ||
				    HasAttribute(Field, typeof(PlayModeOnlyAttribute)))
				{
					return false;
				}
			}
			else if (Field.IsPublic)
			{
				if (Field.IsNotSerialized ||
				    HasAttribute(Field, typeof(PlayModeOnlyAttribute)) ||
				    HasAttribute(Field, typeof(HideInInspector)) ||
				    HasAttribute(Field, typeof(NaughtyAttributes.ReadOnlyAttribute)))
				{
					return false;
				}
			}

			return true;
		}

		public static void RecursiveSearchData(
			HashSet<Component> OnGameObjectComponents,
			HashSet<GameObject> AllGameObjectOnObject,
			IPopulateIDRelation IPopulateIDRelation,
			HashSet<FieldData> FieldDatas,
			string Prefix,
			object PrefabInstance,
			object SpawnedInstance,
			bool UseInstance = false)
		{
			try
			{
				var TypeMono = SpawnedInstance.GetType();
				var coolFields = TypeMono.GetFields(
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
					BindingFlags.FlattenHierarchy
				).ToList();

				foreach (var Field in coolFields) //Loop through found fields
				{
					if (IsGoodField(Field) == false) continue;

					if (Field.Name == "TargetFunction")
					{
						Loggy.LogError("ogggg");
					}


					object APrefabDefault = null;
					if (PrefabInstance != null)
					{
						APrefabDefault = Field.GetValue(PrefabInstance);
					}

					var AMonoSet = Field.GetValue(SpawnedInstance);

					IEnumerable list = null;

					if (Field.FieldType.IsGenericType &&
					    typeof(IEnumerable).IsAssignableFrom(Field.FieldType) &&
					    Field.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>) &&
					    typeof(IDictionary).IsAssignableFrom(Field.FieldType) == false &&
					    typeof(HashSet<>).IsAssignableFrom(Field.FieldType) == false)
					{
						ListHandleSave(AMonoSet, APrefabDefault, Field, FieldDatas, Prefix, UseInstance,
							IPopulateIDRelation, OnGameObjectComponents,
							AllGameObjectOnObject);

						continue;
					}

					if ((typeof(IDictionary<,>).IsAssignableFrom(Field.FieldType) ||
					     typeof(IDictionary).IsAssignableFrom(Field.FieldType)))
					{
						//no Field.FieldType.IsGenericType && due to stupid class dictionary inheritance silly unity stuff
						if (typeof(ISerializationCallbackReceiver)
						    .IsAssignableFrom(Field
							    .FieldType)) //so Serialisable dictionary only, can't directly reference due to assembly stuff
						{
							DictionaryHandleSave(AMonoSet, APrefabDefault, Field, FieldDatas, Prefix, UseInstance,
								IPopulateIDRelation, OnGameObjectComponents, AllGameObjectOnObject);
						}

						continue;
					}


					//if Field is a class and is not related to unity engine.object Serialise it
					if (Field.FieldType.IsValueType == false && Field.FieldType == typeof(string) == false && Field.FieldType.IsGenericType == false &&
					    (APrefabDefault != null || PrefabInstance == null) && AMonoSet != null
					    && Field.FieldType.GetCustomAttributes(typeof(System.SerializableAttribute), true).Length > 0)
					{
						RecursiveSearchData(OnGameObjectComponents, AllGameObjectOnObject,
							IPopulateIDRelation,
							FieldDatas,
							Prefix + Field.Name + "@",
							APrefabDefault,
							AMonoSet,
							UseInstance); //Recursive
						continue;
					}
					else
					{
						if (Field.FieldType == typeof(System.Action)) continue;
						if (Field.FieldType.BaseType == typeof(UnityEngine.Events.UnityEventBase)) continue;

						object PrefabDefault = null;
						if (PrefabInstance != null)
						{
							PrefabDefault = Field.GetValue(PrefabInstance);
						}

						var MonoSet = Field.GetValue(SpawnedInstance);
						if (MonoSet == null) continue;


						if (CheckAreSame(PrefabDefault, MonoSet, OnGameObjectComponents,
							    AllGameObjectOnObject) == false)
						{
							bool IsSO = (Field.FieldType.IsSubclassOf(typeof(ScriptableObject)) &&
							             typeof(IHaveForeverID).IsAssignableFrom(Field.FieldType));

							bool IsComponent = Field.FieldType.IsSubclassOf(typeof(UnityEngine.Component));

							bool anyOfThem = IsComponent || IsSO;

							if (anyOfThem == false)
							{
								anyOfThem = Field.FieldType == typeof(UnityEngine.GameObject);
							}

							if (anyOfThem == false)
							{
								if (Field.FieldType.IsGenericType)
									continue; //Unity editor can't handle this currently so same Functionality
								if (Field.FieldType.GetCustomAttributes(typeof(System.SerializableAttribute), true)
									    .Length == 0) continue;
								if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object))) continue;
							}


							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							SetDataFieldFor(fieldData, MonoSet, IsSO, IsComponent, anyOfThem, Field.FieldType,
								FieldDatas, UseInstance, IPopulateIDRelation, false);
							FieldDatas.Add(fieldData); //add data
						}
					}

					// if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Component)))
					// {
					// 	var Object = Field.GetValue(SpawnedInstance);
					// 	var mono = Object as Component;
					// 	if (mono == null) continue;
					//
					// 	if (OnGameObjectComponents.Contains(mono))
					// 		continue; //Might be controversial but it cleans out Riffraff
					//
					// 	if (mono.transform.parent == null) //is prefab
					// 	{
					// 		var ForeverIDTracker = mono.GetComponent<IHaveForeverID>();
					// 		if (PrefabInstance != null)
					// 		{
					// 			var PrefabSOTracker = (Field.GetValue(PrefabInstance) as Component)
					// 				.GetComponent<IHaveForeverID>();
					// 			if (PrefabSOTracker != null)
					// 			{
					// 				if (PrefabSOTracker.ForeverID == ForeverIDTracker.ForeverID)
					// 				{
					// 					continue;
					// 				}
					// 			}
					// 		}
					//
					// 		if (ForeverIDTracker != null)
					// 		{
					// 			FieldData AfieldData = new FieldData();
					// 			AfieldData.Name = Prefix + Field.Name;
					//
					// 			AfieldData.Data = ForeverIDTracker.ForeverID;
					// 			AfieldData.Data = JsonConvert.SerializeObject(new PrefabComponent()
					// 			{
					// 				ForeverId = ForeverIDTracker.ForeverID,
					// 				ComponentName = mono.GetType().Name
					// 			});
					// 			AfieldData.IsPrefabID = true;
					// 			FieldDatas.Add(AfieldData); //add data
					// 		}
					//
					// 		continue; //is prefab instance
					// 	}
					//
					// 	var fieldData = new FieldData();
					// 	fieldData.Name = Prefix + Field.Name;
					// 	IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono,
					// 		UseInstance); //Callout
					// 	continue;
					// }
					//
					// if (Field.FieldType == typeof(UnityEngine.GameObject))
					// {
					// 	var Object = Field.GetValue(SpawnedInstance);
					// 	var mono = Object as GameObject;
					// 	if (mono == null) continue;
					//
					// 	if (mono.transform.parent == null) //is prefab
					// 	{
					// 		var ForeverIDTracker = mono.GetComponent<IHaveForeverID>();
					// 		if (PrefabInstance != null)
					// 		{
					// 			try
					// 			{
					// 				var PrefabSOTracker = (Field.GetValue(PrefabInstance) as GameObject)
					// 					.GetComponent<IHaveForeverID>();
					// 				if (PrefabSOTracker != null)
					// 				{
					// 					if (PrefabSOTracker.ForeverID == ForeverIDTracker.ForeverID)
					// 					{
					// 						continue;
					// 					}
					// 				}
					// 			}
					// 			catch (Exception e)
					// 			{
					// 				Console.WriteLine(e);
					// 				throw;
					// 			}
					// 		}
					//
					// 		if (ForeverIDTracker != null)
					// 		{
					// 			FieldData AfieldData = new FieldData();
					// 			AfieldData.Name = Prefix + Field.Name;
					// 			AfieldData.Data = ForeverIDTracker.ForeverID;
					// 			AfieldData.IsPrefabID = true;
					// 			FieldDatas.Add(AfieldData); //add data
					// 		}
					// 	}
					//
					// 	//TODO Game object references
					// 	continue;
					// }
					//
					// if (Field.FieldType.IsSubclassOf(typeof(ScriptableObject)) &&
					//     typeof(IHaveForeverID).IsAssignableFrom(Field.FieldType))
					// {
					// 	var SOTracker = Field.GetValue(SpawnedInstance) as IHaveForeverID;
					//
					//
					// 	IHaveForeverID PrefabSOTracker = null;
					// 	if (PrefabInstance != null)
					// 	{
					// 		PrefabSOTracker = Field.GetValue(PrefabInstance) as IHaveForeverID;
					// 	}
					//
					// 	if (PrefabSOTracker?.ForeverID == SOTracker?.ForeverID)
					// 	{
					// 		continue;
					// 	}
					//
					// 	var fieldData = new FieldData();
					// 	fieldData.Name = Prefix + Field.Name;
					// 	if (SOTracker != null)
					// 	{
					// 		fieldData.Data = SOTracker.ForeverID;
					// 	}
					// 	else
					// 	{
					// 		fieldData.Data = "NULL";
					// 	}
					//
					// 	FieldDatas.Add(fieldData);
					// 	continue;
					// }
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}
		}


		private static void SetDataFieldFor(FieldData FieldData, object SpawnedInstance,
			bool isScriptableObject,
			bool IsComponent, bool anyOfThem,
			Type Type, HashSet<FieldData> FieldDatas, bool UseInstance, IPopulateIDRelation IPopulateIDRelation,
			bool MarkAsRemoved)
		{
			if (SpawnedInstance == null)
			{
				if (MarkAsRemoved)
				{
					FieldData.Data = "#removed#";
				}
				else
				{
					FieldData.Data = "NULL";
				}

				return;
			}

			if (isScriptableObject)
			{
				FieldData.Data = (SpawnedInstance as IHaveForeverID)?.ForeverID;
				return;
			}
			else
			{
				if (anyOfThem)
				{
					if (IsComponent)
					{
						var Component = (SpawnedInstance as Component);
						if (Component.transform.parent == null) //Prefab
						{
							var ForeverID = Component.GetComponent<IHaveForeverID>();
							if (ForeverID != null)
							{
								FieldData.Data = JsonConvert.SerializeObject(new PrefabComponent()
								{
									ForeverId = ForeverID.ForeverID,
									ComponentName = Component.GetType().Name
								});
								FieldData.IsPrefabID = true;
								return;
							}
						}
						else
						{
							IPopulateIDRelation.PopulateIDRelation(FieldDatas, FieldData, Component,
								UseInstance); //Callout
							return;
						}
					}
					else
					{
						var GameObjectModified = (SpawnedInstance as GameObject);
						if (GameObjectModified != null && GameObjectModified.transform.parent == null) //Prefab
						{
							var ForeverID = GameObjectModified.GetComponent<IHaveForeverID>();
							if (ForeverID != null)
							{
								FieldData.Data = ForeverID.ForeverID;
								FieldData.IsPrefabID = true;
								return;
							}
						}
						else
						{
							//TODO Support game objects
						}
					}
				}
				else
				{
					FieldData.Data = Librarian.Page.Serialise(SpawnedInstance, Type);
					return;
				}
			}

			FieldData.Data = ""; //Not compatible
			return;
		}

		private static List<object> IEnumeratorToList(IEnumerator IEnumerable)
		{
			var List = new List<object>();
			try
			{
				if (IEnumerable.MoveNext() == false)
				{
					return List;
				}

				var Element = IEnumerable.Current;
				while (true)
				{
					List.Add(Element);
					if (IEnumerable.MoveNext())
					{
						Element = IEnumerable.Current;
					}
					else
					{
						break;
					}
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}


			return List;
		}

		private static bool CheckAreSame(object PrefabDefault, object MonoSet,
			HashSet<Component> OnGameObjectComponents, HashSet<GameObject> AllGameObjectOnObject)
		{
			var selfValueComparer = PrefabDefault as IComparable;
			if (PrefabDefault == null && MonoSet == null)
			{
				return true;
			}
			else if ((PrefabDefault == null && MonoSet != null) || (PrefabDefault != null && MonoSet == null))
			{
				if (MonoSet != null)
				{
					if (MonoSet is Component TMonoComponent)
					{
						if (OnGameObjectComponents.Contains(TMonoComponent))
							return true; //Ignore because is instance on the same object
					}

					if (MonoSet is GameObject TMonoGameObject)
					{
						if (AllGameObjectOnObject.Contains(TMonoGameObject))
							return true; //Ignore because is instance on the same object
					}
				}


				return false; //One is null and the other wasn't
			}

			if (MonoSet is ScriptableObject and IHaveForeverID IHaveForeverIDSO)
			{
				if (IHaveForeverIDSO.ForeverID == (PrefabDefault as IHaveForeverID)?.ForeverID)
				{
					return true;
				}
				else
				{
					return false;
				}
			}


			if (MonoSet is Component MonoComponent && MonoComponent != null)
			{
				if (OnGameObjectComponents.Contains(MonoSet))
					return true; //Ignore because is instance on the same object

				if (MonoComponent.transform.parent == null) //is Prefab
				{
					var MonoIHaveForeverID = MonoComponent.GetComponent<IHaveForeverID>();
					if (MonoIHaveForeverID != null)
					{
						var PrefabIHaveForeverID = (PrefabDefault as Component)?.GetComponent<IHaveForeverID>();
						if (PrefabIHaveForeverID == null)
						{
							return true; //idk What this is but I can't handle it Being different
						}

						if (MonoIHaveForeverID.ForeverID == PrefabIHaveForeverID?.ForeverID)
						{
							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						return true; //idk What this is but I can't handle it Being different
					}
				}

				return false; //Assumed to be external reference
			}

			if (MonoSet is GameObject MonoGameObject && MonoGameObject != null)
			{
				if (AllGameObjectOnObject.Contains(MonoSet))
					return true; //Ignore because is instance on the same object

				if (MonoGameObject.transform.parent == null) //is Prefab
				{
					var MonoIHaveForeverID = MonoGameObject.GetComponent<IHaveForeverID>();
					if (MonoIHaveForeverID != null)
					{
						var PrefabIHaveForeverID =
							(PrefabDefault as GameObject)?.GetComponent<IHaveForeverID>();
						if (PrefabIHaveForeverID == null)
						{
							return true; //idk What this is but I can't handle it Being different
						}

						if (MonoIHaveForeverID.ForeverID == PrefabIHaveForeverID?.ForeverID)
						{
							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						return true; //idk What this is but I can't handle it Being different
					}
				}

				return false; //Assumed to be external reference
			}


			if (selfValueComparer != null && selfValueComparer.CompareTo(MonoSet) != 0)
			{
				return false; //the comparison using IComparable failed
			}
			else if (PrefabDefault.Equals(MonoSet) == false)
			{
				return false; //Using the overridden one
			}
			else if (object.Equals(PrefabDefault, MonoSet) == false)
			{
				return false; //Using the Inbuilt one
			}
			else
			{
				return true; // match
			}
		}
	}
}