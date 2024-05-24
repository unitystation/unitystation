using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logs;
using SecureStuff;
using UnityEngine;
using System.Linq;
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
		private static void ListHandle(string RootID, Component Root, FieldInfo Field, object Object, FieldData ModField, int Index,
			IPopulateIDRelation IPopulateIDRelation, bool AllLoaded = false, bool IsServer = true)
		{
			var List = (Field.GetValue(Object) as IList);
			if (typeof(GameObject).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
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
					if (ModField.Data == "#removed#")
					{
						List.Remove(Index);
					}
					else
					{
						while (List.Count <= Index)
							//TODO Could be exploited? well You could just have a map with a million objects so idk xD
						{
							List.Add(null);
						}

						var Prefab = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data,
							Field.FieldType.GetGenericArguments()[0]);

						List[Index] = ((GameObject) Prefab);
					}
				}
				else
				{
					Loggy.LogError("Needs to be added!!!");
					//TODO Implement!!
				}
			}
			else if (typeof(Component).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
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
					if (ModField.Data == "#removed#")
					{
						List.Remove(Index);
					}
					else
					{
						while (List.Count <= Index)
							//TODO Could be exploited? well You could just have a map with a million objects so idk xD
						{
							List.Add(null);
						}

						var PrefabComponent = JsonConvert.DeserializeObject<PrefabComponent>(ModField.Data);
						try
						{
							var Prefab = IPopulateIDRelation.ObjectsFromForeverID(PrefabComponent.ForeverId,
								Field.FieldType.GetGenericArguments()[0]);
							List[Index] = ((GameObject) Prefab).GetComponent(PrefabComponent.ComponentName);
						}
						catch (Exception e)
						{
							Loggy.LogError(e.ToString());
						}
					}
				}
				else
				{
					if (AllLoaded)
					{
						while (List.Count <= Index)
							//TODO Could be exploited? well You could just have a map with a million objects so idk xD
						{
							List.Add(null);
						}

						List[Index] = GetComponentPath(ModField.Data, IPopulateIDRelation);
					}
					else
					{
						IPopulateIDRelation.FlagSaveKey(RootID, Root, ModField);
					}
				}
			}
			else if (typeof(ScriptableObject).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]) &&
			         typeof(IHaveForeverID).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
			{
				if (ModField.Data == "#removed#")
				{
					List.Remove(Index);
				}
				else
				{
					if (List == null)
					{
						Loggy.LogError("0oh no...");
					}
					while (List.Count <= Index)
						//TODO Could be exploited? well You could just have a map with a million objects so idk xD
					{
						List.Add(null);
					}

					var SO = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data,
						Field.FieldType.GetGenericArguments()[0]);
					List[Index] = SO;
				}
			}
			else
			{
				if (ModField.Data == "#removed#")
				{
					List.Remove(Index);
				}
				else
				{
					while (List.Count <= Index)
						//TODO Could be exploited? well You could just have a map with a million objects so idk xD
					{
						List.Add(null);
					}

					List[Index] = Librarian.Page.DeSerialiseValue(ModField.Data,
						Field.FieldType.GetGenericArguments()[0]);
				}
			}
		}

		private static GameObject GetGameObjectPath(string Id, IPopulateIDRelation IPopulateIDRelation)
		{
			if (Id == "MISSING")
			{
				Loggy.LogError("Map has missing references");
				return null;
			}

			var IDPath = Id.Split("@");
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

		private static Component GetComponentPath(string Id, IPopulateIDRelation IPopulateIDRelation)
		{
			if (Id == "MISSING")
			{
				Loggy.LogError("Map has missing references");
				return null;
			}

			var IDPath = Id.Split("@");
			var Object = GetGameObjectPath(Id, IPopulateIDRelation);
			return Object.GetComponent(IDPath[2]); //TODO Support multiple;
		}

		private static void ProcessIndividualField(string RootID, Component root, object Object, FieldData ModField,
			IPopulateIDRelation IPopulateIDRelation,
			bool AllLoaded = false,
			string AppropriateName = "", bool IsServer = true)
		{
			var TypeMono = Object.GetType();
			int Index = 0;

			string AdditionalJumps = "";
			if (string.IsNullOrEmpty(AppropriateName))
			{
				AppropriateName = ModField.Name;
			}


			if (AppropriateName.Contains("@")) //TODO do @ and # Collide?
			{
				var NewPath = AppropriateName.Split("@", 2);
				AdditionalJumps = NewPath[1];
				AppropriateName = NewPath[0];
			}


			if (AppropriateName.Contains("#"))
			{
				var Split = AppropriateName.Split("#");
				AppropriateName = Split[0];
				Index = int.Parse(Split[1]);
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

				if (Field.FieldType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(Field.FieldType) &&
				    Field.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
				{
					bool isScriptableObject = false;
					bool IsComponent = false;

					bool anyOfThem = false;

					if (typeof(GameObject).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
					{
						anyOfThem = true;
					}
					else if (typeof(Component).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
					{
						anyOfThem = true;
					}
					else if (typeof(ScriptableObject).IsAssignableFrom(
						         Field.FieldType.GetGenericArguments()[0]) &&
					         typeof(IHaveForeverID).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
					{
						anyOfThem = true;
					}

					if (anyOfThem)
					{
						ListHandle(RootID, root, Field, Object, ModField, Index, IPopulateIDRelation, AllLoaded);
					}
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
							IPopulateIDRelation.ObjectsFromForeverID(PrefabComponent.ForeverId, Field.FieldType);

						Field.SetValue(Object, ((GameObject) Prefab)?.GetComponent(PrefabComponent.ComponentName));
					}
					else
					{
						if (AllLoaded)
						{
							Field.SetValue(Object, GetComponentPath(ModField.Data, IPopulateIDRelation));
						}
						else
						{
							IPopulateIDRelation.FlagSaveKey(RootID, root, ModField);
						}
					}
				}

				if (Field.FieldType == typeof(UnityEngine.GameObject))
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
						var Prefab = IPopulateIDRelation.ObjectsFromForeverID(ModField.Data, Field.FieldType);
						Field.SetValue(Object, ((GameObject) Prefab));
					}
					else
					{
						if (AllLoaded)
						{
							Field.SetValue(Object, GetGameObjectPath(ModField.Data, IPopulateIDRelation));
						}
						else
						{
							IPopulateIDRelation.FlagSaveKey(RootID, root, ModField);
						}
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
					ProcessIndividualField(RootID, root, Field.GetValue(Object), ModField, IPopulateIDRelation, AllLoaded, AdditionalJumps);
					return;
				}
			}


			if (Field.FieldType.IsGenericType &&
			    Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return; //skipping all dictionaries For now
			if (Field.FieldType == typeof(System.Action)) return;
			if (Field.FieldType.BaseType == typeof(UnityEngine.Events.UnityEventBase))
				return; //TODO Handle separately Since it is same as Object references


			var MonoSet = Field.GetValue(Object);
			if (MonoSet == null) return;

			if (Field.FieldType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(Field.FieldType) &&
			    Field.FieldType.GetGenericArguments()[0].IsValueType) //UnityEventBase Handle differently
			{
				ListHandle(RootID, root, Field, Object, ModField, Index, IPopulateIDRelation);
			}
			else
			{
				if (Field.FieldType.IsGenericType) return; //Unity editor can't handle this currently so same Functionality
				Field.SetValue(Object, Librarian.Page.DeSerialiseValue(ModField.Data, Field.FieldType));
			}
		}

		private static void LoadDatarecursive(string RootID,  Component root, object Object, HashSet<FieldData> IndividualObject,
			IPopulateIDRelation IPopulateIDRelation,
			bool AllLoaded = false, bool IsServer = true) //Has to be Component to restrict it from being used in silly places
		{
			try
			{
				if (Object == null)
				{
					Loggy.LogError("oh....");
				}

				foreach (var ModField in IndividualObject)
				{
					try
					{
						ProcessIndividualField(RootID,root, Object, ModField, IPopulateIDRelation, AllLoaded);
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

		public static void LoadData(string RootID,  Component Object, HashSet<FieldData> IndividualObject,
			IPopulateIDRelation IPopulateIDRelation,
			bool AllLoaded = false, bool IsServer =true) //Has to be Component to restrict it from being used in silly places
		{
			if (Object == null) return;
			LoadDatarecursive(RootID,Object, Object, IndividualObject, IPopulateIDRelation, AllLoaded);
		}

		private static bool IsGoodClientField(FieldInfo Field)
		{
			//TODO Work out how to support
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

		private static bool IsGoodField(FieldInfo Field)
		{
			if (Field.IsPrivate || Field.IsAssembly || Field.IsFamily)
			{
				var attribute = Field.GetCustomAttributes(typeof(SerializeField), true);
				if (attribute.Length == 0)
				{
					return false;
				}

				attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
				if (attribute.Length > 0)
				{
					return false;
				}

				attribute = Field.GetCustomAttributes(typeof(NaughtyAttributes.ReadOnlyAttribute), true);
				if (attribute.Length > 0)
				{
					return false;
				}

				attribute = Field.GetCustomAttributes(typeof(PlayModeOnlyAttribute), true);
				if (attribute.Length > 0)
				{
					return false;
				}
			}
			else if (Field.IsPublic)
			{
				if (Field.IsNotSerialized)
				{
					return false;
				}

				var attribute = Field.GetCustomAttributes(typeof(PlayModeOnlyAttribute), true);
				if (attribute.Length > 0)
				{
					return false;
				}

				attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
				if (attribute.Length > 0)
				{
					return false;
				}

				attribute = Field.GetCustomAttributes(typeof(NaughtyAttributes.ReadOnlyAttribute), true);
				if (attribute.Length > 0)
				{
					return false;
				}
			}

			return true;
		}

		public static void RecursiveSearchData(HashSet<Component> OnGameObjectComponents,
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

					// if (Field.Name == "m_dict")
					// {
					// 	Loggy.LogError("ogggg");
					// }

					if (Field.FieldType.IsValueType == false &&
					    (Field.FieldType == typeof(string)) == false) //Cross object references
					{
						object APrefabDefault = null;
						if (PrefabInstance != null)
						{
							APrefabDefault = Field.GetValue(PrefabInstance);
						}

						var AMonoSet = Field.GetValue(SpawnedInstance);


						IEnumerable list = null;

						if (Field.FieldType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(Field.FieldType) &&
						    Field.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
						{
							//&& Field.FieldType.GetGenericArguments()[0]

							bool isScriptableObject = false;
							bool IsComponent = false;

							bool anyOfThem = false;

							if (typeof(GameObject).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
							{
								IsComponent = false;
								isScriptableObject = false;
								anyOfThem = true;
							}
							else if (typeof(Component).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
							{
								IsComponent = true;
								isScriptableObject = false;
								anyOfThem = true;
							}
							else if (typeof(ScriptableObject).IsAssignableFrom(
								         Field.FieldType.GetGenericArguments()[0]) &&
							         typeof(IHaveForeverID).IsAssignableFrom(Field.FieldType.GetGenericArguments()[0]))
							{
								IsComponent = false;
								isScriptableObject = true;
								anyOfThem = true;
							}

							if (anyOfThem)
							{
								var modified = IEnumeratorToList((AMonoSet as IEnumerable).GetEnumerator());

								List<object> original = new List<object>();


								if (APrefabDefault != null)
								{
									original = IEnumeratorToList((APrefabDefault as IEnumerable).GetEnumerator());
								}


								for (int i = 0; i < Math.Max(original.Count, modified.Count); i++)
								{
									if (i < original.Count && i < modified.Count)
									{
										if (CheckAreSame(original[i], modified[i]) == false)
										{
											SaveComponentGameObjectScriptableObject(IsComponent, isScriptableObject,
												Prefix,
												Field, i, modified, FieldDatas, UseInstance, IPopulateIDRelation);
										}
									}
									else if (i < original.Count)
									{
										FieldData fieldData = new FieldData();
										fieldData.Name = Prefix + Field.Name + "#" + i + "#" + "Removed";
										fieldData.Data = "#removed#";
										FieldDatas.Add(fieldData);
									}
									else if (i < modified.Count)
									{
										SaveComponentGameObjectScriptableObject(IsComponent, isScriptableObject, Prefix,
											Field, i, modified, FieldDatas, UseInstance, IPopulateIDRelation);
									}
								}
							}
						}

						if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Component)))
						{
							var Object = Field.GetValue(SpawnedInstance);
							var mono = Object as Component;
							if (mono == null) continue;

							if (OnGameObjectComponents.Contains(mono))
								continue; //Might be controversial but it cleans out Riffraff

							if (mono.transform.parent == null) //is prefab
							{
								var ForeverIDTracker = mono.GetComponent<IHaveForeverID>();
								if (PrefabInstance != null)
								{
									var PrefabSOTracker = (Field.GetValue(PrefabInstance) as Component)
										.GetComponent<IHaveForeverID>();
									if (PrefabSOTracker != null)
									{
										if (PrefabSOTracker.ForeverID == ForeverIDTracker.ForeverID)
										{
											continue;
										}
									}
								}

								if (ForeverIDTracker != null)
								{
									FieldData AfieldData = new FieldData();
									AfieldData.Name = Prefix + Field.Name;

									AfieldData.Data = ForeverIDTracker.ForeverID;
									AfieldData.Data = JsonConvert.SerializeObject(new PrefabComponent()
									{
										ForeverId = ForeverIDTracker.ForeverID,
										ComponentName = mono.GetType().Name
									});
									AfieldData.IsPrefabID = true;
									FieldDatas.Add(AfieldData); //add data
								}

								continue; //is prefab instance
							}

							var fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono,
								UseInstance); //Callout
							continue;
						}

						if (Field.FieldType == typeof(UnityEngine.GameObject))
						{
							var Object = Field.GetValue(SpawnedInstance);
							var mono = Object as GameObject;
							if (mono == null) continue;

							if (mono.transform.parent == null) //is prefab
							{
								var ForeverIDTracker = mono.GetComponent<IHaveForeverID>();
								if (PrefabInstance != null)
								{
									try
									{
										var PrefabSOTracker = (Field.GetValue(PrefabInstance) as GameObject).GetComponent<IHaveForeverID>();
										if (PrefabSOTracker != null)
										{
											if (PrefabSOTracker.ForeverID == ForeverIDTracker.ForeverID)
											{
												continue;
											}
										}
									}
									catch (Exception e)
									{
										Console.WriteLine(e);
										throw;
									}
								}

								if (ForeverIDTracker != null)
								{
									FieldData AfieldData = new FieldData();
									AfieldData.Name = Prefix + Field.Name;
									AfieldData.Data = ForeverIDTracker.ForeverID;
									AfieldData.IsPrefabID = true;
									FieldDatas.Add(AfieldData); //add data
								}
							}

							//TODO Game object references
							continue;
						}

						if (Field.FieldType.IsSubclassOf(typeof(ScriptableObject)) &&
						    typeof(IHaveForeverID).IsAssignableFrom(Field.FieldType))
						{
							var SOTracker = Field.GetValue(SpawnedInstance) as IHaveForeverID;


							IHaveForeverID PrefabSOTracker = null;
							if (PrefabInstance != null)
							{
								PrefabSOTracker = Field.GetValue(PrefabInstance) as IHaveForeverID;
							}

							if (PrefabSOTracker?.ForeverID == SOTracker?.ForeverID)
							{
								continue;
							}

							var fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							if (SOTracker != null)
							{
								fieldData.Data = SOTracker.ForeverID;
							}
							else
							{
								fieldData.Data = "NULL";
							}

							FieldDatas.Add(fieldData);
							continue;
						}

						//if Field is a class and is not related to unity engine.object Serialise it
						if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object))) continue;

						if (Field.FieldType.IsGenericType == false &&
						    (APrefabDefault != null || PrefabInstance == null) && AMonoSet != null)
						{
							RecursiveSearchData(OnGameObjectComponents, AllGameObjectOnObject, IPopulateIDRelation,
								FieldDatas,
								Prefix + Field.Name + "@",
								APrefabDefault,
								AMonoSet,
								UseInstance); //Recursive
							continue;
						}
					}


					if (Field.FieldType.IsGenericType &&
					    Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
						continue; //skipping all dictionaries For now
					if (Field.FieldType == typeof(System.Action)) continue;
					if (Field.FieldType.BaseType == typeof(UnityEngine.Events.UnityEventBase))
						continue; //TODO Handle separately Since it is same as Object references


					object PrefabDefault = null;
					if (PrefabInstance != null)
					{
						PrefabDefault = Field.GetValue(PrefabInstance);
					}

					var MonoSet = Field.GetValue(SpawnedInstance);
					if (MonoSet == null) continue;

					if (Field.FieldType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(Field.FieldType) &&
					    Field.FieldType.GetGenericArguments()[0].IsValueType) //UnityEventBase Handle differently
					{
						var modified = IEnumeratorToList((MonoSet as IEnumerable).GetEnumerator());

						List<object> original = new List<object>();


						if (PrefabDefault != null)
						{
							original = IEnumeratorToList((PrefabDefault as IEnumerable).GetEnumerator());
						}


						for (int i = 0; i < Math.Max(original.Count, modified.Count); i++)
						{
							if (i < original.Count && i < modified.Count)
							{
								if (CheckAreSame(original[i], modified[i]) == false)
								{
									FieldData fieldData = new FieldData();
									fieldData.Name = Prefix + Field.Name + "#" + i;
									fieldData.Data = modified[i]?.ToString();
									FieldDatas.Add(fieldData); //add data
								}
							}
							else if (i < original.Count)
							{
								FieldData fieldData = new FieldData();
								fieldData.Name = Prefix + Field.Name + "#" + i + "#" + "Removed";
								fieldData.Data = "#removed#";
								FieldDatas.Add(fieldData);
							}
							else if (i < modified.Count)
							{
								FieldData fieldData = new FieldData();
								fieldData.Name = Prefix + Field.Name + "#" + i;
								fieldData.Data = modified[i]?.ToString();
								FieldDatas.Add(fieldData); //add data
							}
						}
					}
					else
					{
						if (Field.FieldType.IsGenericType)
							continue; //Unity editor can't handle this currently so same Functionality
						if (CheckAreSame(PrefabDefault, MonoSet) == false)
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							fieldData.Data = Librarian.Page.Serialise(MonoSet, Field.FieldType);
							FieldDatas.Add(fieldData); //add data
						}
					}

					//if is a Variables inside of the class will be flattened with field name of class@Field name
					//Better if recursiveThrough the class

					//Don't do sub- variables in struct
					//If it is a class,
					//Is class Is thing thing,
					//and then Just repeat the loop but within that class with the added notation
				}
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}
		}


		private static void SaveComponentGameObjectScriptableObject(
			bool IsComponent,
			bool isScriptableObject,
			string Prefix,
			FieldInfo Field,
			int i,
			List<object> modified,
			HashSet<FieldData> FieldDatas,
			bool UseInstance,
			IPopulateIDRelation IPopulateIDRelation)
		{
			if (isScriptableObject)
			{
				FieldData fieldData = new FieldData();
				fieldData.Name = Prefix + Field.Name + "#" + i;
				fieldData.Data = (modified[i] as IHaveForeverID)?.ForeverID;
				FieldDatas.Add(fieldData); //add data
			}
			else
			{
				if (IsComponent)
				{
					var Component = (modified[i] as Component);
					if (Component.transform.parent == null) //Prefab
					{
						var ForeverID = Component.GetComponent<IHaveForeverID>();
						if (ForeverID != null)
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name + "#" + i;
							fieldData.Data = JsonConvert.SerializeObject(new PrefabComponent()
							{
								ForeverId = ForeverID.ForeverID,
								ComponentName = Component.GetType().Name
							});
							fieldData.IsPrefabID = true;
							FieldDatas.Add(fieldData); //add data
						}
					}
					else
					{
						var mono = modified[i] as Component;
						var fieldData = new FieldData();
						fieldData.Name = Prefix + Field.Name + "#" + i;
						IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono, UseInstance); //Callout
					}
				}
				else
				{

					var GameObjectModified = (modified[i] as GameObject);
					if (GameObjectModified != null && GameObjectModified.transform.parent == null) //Prefab
					{
						var ForeverID = GameObjectModified.GetComponent<IHaveForeverID>();
						if (ForeverID != null)
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name + "#" + i;
							fieldData.Data = ForeverID.ForeverID;
							fieldData.IsPrefabID = true;
							FieldDatas.Add(fieldData); //add data
						}
					}
					else
					{
						object data = null;
						if (modified[i] != null)
						{
							data = modified[i];
						}

						Loggy.LogError(
							$"Unimplemented Referencing game objects in Scenes {data?.ToString()} with Field {Field.Name}");

						//TODO
						// var mono = modified[i] as Component;
						// var fieldData = new FieldData();
						// fieldData.Name = Prefix + Field.Name;
						// IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono,
						// UseInstance); //Callout
					}
				}
			}
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

		private static bool CheckAreSame(object PrefabDefault, object MonoSet)
		{
			var selfValueComparer = PrefabDefault as IComparable;
			if (PrefabDefault == null && MonoSet == null)
			{
				return true;
			}
			else if ((PrefabDefault == null && MonoSet != null) || (PrefabDefault != null && MonoSet == null))
			{
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


			if (MonoSet is Component MonoComponent)
			{
				if (MonoComponent.transform.parent == null) //is Prefab
				{
					var MonoIHaveForeverID = MonoComponent.GetComponent<IHaveForeverID>();
					if (MonoIHaveForeverID != null)
					{
						var PrefabIHaveForeverID = (PrefabDefault as Component)?.GetComponent<IHaveForeverID>();
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
						return false;
					}
				}
			}

			if (MonoSet is GameObject MonoGameObject)
			{
				if (MonoGameObject.transform.parent == null) //is Prefab
				{
					var MonoIHaveForeverID = MonoGameObject.GetComponent<IHaveForeverID>();
					if (MonoIHaveForeverID != null)
					{
						var PrefabIHaveForeverID = (PrefabDefault as GameObject)?.GetComponent<IHaveForeverID>();
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
						return false;
					}
				}
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