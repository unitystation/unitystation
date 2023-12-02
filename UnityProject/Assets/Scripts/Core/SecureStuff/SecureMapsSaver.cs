using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Logs;
using SecureStuff;
using UnityEngine;
using System.Linq;
using NUnit.Compatibility;
using UnityEngine.Events;

namespace SecureStuff
{
	public interface IPopulateIDRelation
	{
		public void PopulateIDRelation(HashSet<FieldData> FieldDatas, FieldData fieldData, Component mono,
			bool UseInstance = false);
	}


	public class SceneObjectReference : BaseAttribute
	{
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
			foreach (var ID in ReferencingIDs)
			{
				Data = Data + "," + ID;
			}
		}

		public string Name;
		public string Data;
	}

	public static class SecureMapsSaver
	{
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
					if (Field.IsPrivate || Field.IsAssembly || Field.IsFamily)
					{
						var attribute = Field.GetCustomAttributes(typeof(SerializeField), true);
						if (attribute.Length == 0)
						{
							continue;
						}

						attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
						if (attribute.Length > 0)
						{
							continue;
						}

						attribute = Field.GetCustomAttributes(typeof(NaughtyAttributes.ReadOnlyAttribute), true);
						if (attribute.Length > 0)
						{
							continue;
						}
					}
					else if (Field.IsPublic)
					{
						if (Field.IsNotSerialized)
						{
							continue;
						}

						var attribute = Field.GetCustomAttributes(typeof(PlayModeOnlyAttribute), true);
						if (attribute.Length > 0)
						{
							continue;
						}

						attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
						if (attribute.Length > 0)
						{
							continue;
						}

						attribute = Field.GetCustomAttributes(typeof(NaughtyAttributes.ReadOnlyAttribute), true);
						if (attribute.Length > 0)
						{
							continue;
						}
					}

					if (Field.FieldType.IsValueType == false && (Field.FieldType == typeof(string)) == false) //Cross object references
					{
						object APrefabDefault = null;
						if (PrefabInstance != null)
						{
							APrefabDefault = Field.GetValue(PrefabInstance);
						}
						var AMonoSet = Field.GetValue(SpawnedInstance);

						IEnumerable list = null;

						if (Field.FieldType.IsGenericType)
						{
							list = AMonoSet as IEnumerable;
							if (list != null)
							{
								bool isListCompatible = false;
								foreach (var Item in list)
								{
									if (Item == null) continue;
									if (Item.GetType().IsSubclassOf(typeof(UnityEngine.Object)) &&
									    Item is Component)
									{
										isListCompatible = true;
									}

									break;
								}

								if (isListCompatible)
								{
									var fieldData = new FieldData();
									fieldData.Name = Prefix + Field.Name;
									foreach (var Item in list)
									{
										var mono = Item as Component;
										var gameObject = Item as GameObject;

										if (gameObject != null)
										{
											if (gameObject.transform.parent == null) continue; //is prefab insta
											if (AllGameObjectOnObject.Contains(gameObject) == false)
											{
												Loggy.LogError(" Game object referenced  " + gameObject.name);
											}
										}


										if (mono == null) continue;

										if (mono.transform.parent == null) continue; //is prefab instance

										IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono,
											UseInstance); //Callout
									}

									continue;
								}
							}
						}

						//TODO SO Handling

						if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
						{
							var Object = Field.GetValue(SpawnedInstance);
							var mono = Object as Component;
							var gameObject = Object as GameObject;

							if (gameObject != null)
							{
								if (gameObject.transform.parent == null) continue; //is prefab instance
								if (AllGameObjectOnObject.Contains(gameObject) == false)
								{
									Loggy.LogError(" Game object referenced  " + gameObject.name);
								}
							}


							if (mono == null) continue;

							if (OnGameObjectComponents.Contains(mono)) continue; //Might be controversial but it cleans out Riffraff

							if (mono.transform.parent == null) continue; //is prefab instance

							var fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono,
								UseInstance); //Callout
							continue;
						}


						//if Field is a class and is not related to unity engine.object Serialise it
						if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object))) continue;

						if (Field.FieldType.IsGenericType == false && (APrefabDefault != null || PrefabInstance == null) && AMonoSet != null)
						{
							RecursiveSearchData(OnGameObjectComponents, AllGameObjectOnObject , IPopulateIDRelation, FieldDatas,
								Prefix + Field.Name + "@",
								APrefabDefault,
								AMonoSet,
								UseInstance); //Recursive
							continue;
						}
					}

					if (Field.FieldType.IsGenericType && Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) continue; //skipping all dictionaries For now
					if (Field.FieldType == typeof(System.Action)) continue;
					if (Field.FieldType.BaseType == typeof(UnityEngine.Events.UnityEventBase)) continue; //TODO Handle separately Since it is same as Object references


					object PrefabDefault = null;
					if (PrefabInstance != null)
					{
						PrefabDefault = Field.GetValue(PrefabInstance);
					}
					var MonoSet = Field.GetValue(SpawnedInstance);
					if (MonoSet == null) continue;

					if (Field.FieldType.IsGenericType && (MonoSet as IEnumerable) != null && Field.FieldType.GetGenericArguments()[0].IsValueType) //UnityEventBase Handle differently
					{
						var modified  = IEnumeratorToList((MonoSet as IEnumerable).GetEnumerator());

						List<object> original = new List<object>();


						if (PrefabDefault != null)
						{
							original = IEnumeratorToList((PrefabDefault as IEnumerable).GetEnumerator());
						}


						for (int i = 0; i < Math.Max(original.Count, modified.Count); i++)
						{
							if (i < original.Count && i < modified.Count)
							{
								if (CheckAreSame(original[i] , modified[i]) == false)
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
						if (Field.FieldType.IsGenericType) continue; //Unity editor can't handle this currently so same Functionality
						if (CheckAreSame(PrefabDefault, MonoSet) == false)
						{
							FieldData fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							fieldData.Data = MonoSet.ToString();
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
			bool areSame;
			if (PrefabDefault == null && MonoSet == null)
			{
				return  true;
			}
			else if ((PrefabDefault == null && MonoSet != null) || (PrefabDefault != null && MonoSet == null))
			{
				return  false; //One is null and the other wasn't
			}
			else if (selfValueComparer != null && selfValueComparer.CompareTo(MonoSet) != 0)
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