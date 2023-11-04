using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HealthV2
{
	[CreateAssetMenu(fileName = "SurgeryProcedureBaseSingleton", menuName = "ScriptableObjects/Surgery/SurgeryProcedureBaseSingleton")]
	public class SurgeryProcedureBaseSingleton : SingletonScriptableObject<SurgeryProcedureBaseSingleton>
	{
		public List<SurgeryProcedureBase> StoredReferences = new List<SurgeryProcedureBase>();

#if UNITY_EDITOR
		[NaughtyAttributes.Button()]
		public void FindAll()
		{

			StoredReferences.Clear();
			EditorUtility.SetDirty(this);
			var SurgeryProcedureBases = FindAssetsByType<SurgeryProcedureBase>();
			StoredReferences.AddRange(SurgeryProcedureBases);
			EditorUtility.SetDirty(this);
			Undo.RecordObject(this, "AlertSOs FindAll ");
		}

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
#endif
	}
}