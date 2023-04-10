using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

[CreateAssetMenu(fileName = "AlertSOs", menuName = "Singleton/AlertSOs")]
public class AlertSOs : SingletonScriptableObject<AlertSOs>
{
	public List<AlertSO> AllAlertSOs = new List<AlertSO>();




#if UNITY_EDITOR
	[NaughtyAttributes.Button()]
	public void FindAll()
	{

		AllAlertSOs.Clear();
		EditorUtility.SetDirty(this);
		var AlertSOss = FindAssetsByType<AlertSO>();
		AllAlertSOs.AddRange(AlertSOss);
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
