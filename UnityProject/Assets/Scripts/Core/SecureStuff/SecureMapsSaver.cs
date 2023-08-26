using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SecureStuff;
using UnityEngine;

namespace SecureStuff
{
	public interface IPopulateIDRelation
	{
		public void PopulateIDRelation(HashSet<FieldData> FieldDatas, FieldData fieldData, MonoBehaviour mono, bool UseInstance = false);
	}


	public class SceneObjectReference : BaseAttribute
	{
	}


	public class FieldData
	{
		private List<string> ReferencingIDs;

		private List<MonoBehaviour> RuntimeReferences;

		public virtual List<MonoBehaviour> GetRuntimeReferences()
		{
			return RuntimeReferences;
		}

		public virtual void RemoveRuntimeReference(MonoBehaviour inRuntimeReference)
		{
			if (RuntimeReferences == null)
			{
				return;
			}

			RuntimeReferences.Remove(inRuntimeReference);
		}

		public virtual void AddRuntimeReference(MonoBehaviour inRuntimeReference)
		{
			if (RuntimeReferences == null)
			{
				RuntimeReferences = new List<MonoBehaviour>();
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
		public static void RecursiveSearchData(IPopulateIDRelation IPopulateIDRelation, HashSet<FieldData> FieldDatas,
			string Prefix, object PrefabInstance,
			object SpawnedInstance, bool UseInstance = false)
		{
			var TypeMono = PrefabInstance.GetType();
			var coolFields = TypeMono.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy
			).ToList();

			foreach (var Field in coolFields) //Loop through found fields
			{
				if (Field.IsPrivate || Field.IsAssembly || Field.IsFamily)
				{
					var attribute = Field.GetCustomAttributes(typeof( SerializeField), true);
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

					var attribute = Field.GetCustomAttributes(typeof(HideInInspector), true);
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

				if (Field.FieldType.IsValueType == false && (Field.FieldType == typeof(string)) == false)
				{
					var APrefabDefault = Field.GetValue(PrefabInstance);
					var AMonoSet = Field.GetValue(SpawnedInstance);

					IEnumerable list = null;
					var Coolattribute = Field.GetCustomAttributes(typeof(SceneObjectReference), true);
					if (Coolattribute.Length > 0)
					{
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
									    Item is MonoBehaviour)
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
										var mono = Item as MonoBehaviour;
										if (mono == null) continue;

										IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono,
											UseInstance); //Callout
									}
								}

								continue;
							}
						}

						if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
						{
							var mono = Field.GetValue(SpawnedInstance) as MonoBehaviour;
							if (mono == null) continue;
							var fieldData = new FieldData();
							fieldData.Name = Prefix + Field.Name;
							IPopulateIDRelation.PopulateIDRelation(FieldDatas, fieldData, mono, UseInstance); //Callout
						}

						continue;
					}

					//if Field is a class and is not related to unity engine.object Serialise it
					if (Field.FieldType.IsSubclassOf(typeof(UnityEngine.Object))) continue;

					if (APrefabDefault != null && AMonoSet != null)
					{
						RecursiveSearchData(IPopulateIDRelation, FieldDatas, Prefix + Field.Name + "@", APrefabDefault,
							AMonoSet,
							UseInstance); //Recursive
						continue;
					}
				}

				if (Field.FieldType.IsGenericType && Field.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
				   ) continue; //skipping all dictionaries For now
				if (Field.FieldType == typeof(System.Action)) continue;


				var PrefabDefault = Field.GetValue(PrefabInstance);
				var MonoSet = Field.GetValue(SpawnedInstance);

				if (MonoSet == null) continue;

				var selfValueComparer = PrefabDefault as IComparable;
				bool areSame;
				if (PrefabDefault == null && MonoSet == null)
				{
					areSame = true;
				}
				else if ((PrefabDefault == null && MonoSet != null) || (PrefabDefault != null && MonoSet == null))
				{
					areSame = false; //One is null and the other wasn't
				}
				else if (selfValueComparer != null && selfValueComparer.CompareTo(MonoSet) != 0)
				{
					areSame = false; //the comparison using IComparable failed
				}
				else if (PrefabDefault.Equals(MonoSet) == false)
				{
					areSame = false; //Using the overridden one
				}
				else if (object.Equals(PrefabDefault, MonoSet) == false)
				{
					areSame = false; //Using the Inbuilt one
				}
				else
				{
					areSame = true; // match
				}


				if (areSame == false)
				{
					FieldData fieldData = new FieldData();
					fieldData.Name = Prefix + Field.Name;
					fieldData.Data = MonoSet.ToString();
					FieldDatas.Add(fieldData); //add data
				}
				//if is a Variables inside of the class will be flattened with field name of class@Field name
				//Better if recursiveThrough the class

				//Don't do sub- variables in struct
				//If it is a class,
				//Is class Is thing thing,
				//and then Just repeat the loop but within that class with the added notation
			}
		}
	}
}