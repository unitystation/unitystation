using System;
using SecureStuff;
using UnityEditor;
using UnityEngine;
using Util;
#if UNITY_EDITOR
#endif

namespace US13.UI.Core
{
	public class PrefabTracker : MonoBehaviour, IHaveForeverID
	{

		[SerializeField] private string parentID;
		public string ParentID
		{
			get {
#if UNITY_EDITOR
				if (string.IsNullOrEmpty(parentID))
				{
					ReassignParentID();
					try
					{
						PrefabUtility.SavePrefabAsset(this.gameObject);
					}
					catch (Exception) { }
				}
#endif
				return parentID;
			}
			set => parentID = value;
		}


		public string ForeverID {
			get {
#if UNITY_EDITOR
				if (string.IsNullOrEmpty(foreverID))
				{
					ReassignID();
					try
					{
						PrefabUtility.SavePrefabAsset(this.gameObject);
					}
					catch (Exception) { }
				}
#endif
				return foreverID;
			}
			set => foreverID = value;
		}

		[SerializeField] private string foreverID;
		[field:SerializeField] public string AlternativePrefabName { get; private set; }
		[field: SerializeField] public bool CanBeSpawnedByAdmin { get; private set; } = true;

		public string GetUnmodifiedParentID()
		{
			return parentID;
		}

		[NaughtyAttributes.Button("get Parent ID")]
		public void ReassignParentID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			var obs = PrefabUtility.GetCorrespondingObjectFromSource(this.gameObject);
			if (obs != null)
			{
				var Tracker = obs.GetComponent<PrefabTracker>();
				if (Tracker != null)
				{
					parentID = Tracker.ForeverID;
				}
				else
				{
					parentID = "null";
				}

			}
			else
			{
				parentID = "root";
			}
			EditorUtility.SetDirty(gameObject);
#endif
		}

		public void ReassignID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			foreverID =
				AssetDatabase.AssetPathToGUID(
					AssetDatabase.GetAssetPath(gameObject)); //Can possibly change over time so need some prevention
			if (string.IsNullOrEmpty(foreverID))
			{
				foreverID = CreateString(20);
			}
#endif
		}

		[NaughtyAttributes.Button("Assign random ID")]
		public void ForceSetID() //Assuming it's a prefab Variant
		{
#if UNITY_EDITOR
			// Can possibly change over time so need some prevention
			foreverID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(gameObject));
			if (string.IsNullOrEmpty(foreverID))
			{
				foreverID = CreateString(20);
			}

			EditorUtility.SetDirty(gameObject);
#endif
		}
#if UNITY_EDITOR
		[NaughtyAttributes.Button("Assign Name as ID")]
		public void ForceSetIDName()
		{

			// Can possibly change over time so need some prevention
			foreverID = this.gameObject.name;
			EditorUtility.SetDirty(gameObject);

		}
#endif
		internal static string CreateString(int stringLength)
		{
			const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!$?_-";
			char[] chars = new char[stringLength];

			for (int i = 0; i < stringLength; i++)
			{
				chars[i] = allowedChars.PickRandom();
			}

			return new string(chars);
		}
	}
}
