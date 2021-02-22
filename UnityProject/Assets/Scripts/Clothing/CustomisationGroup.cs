using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerCustomisationGroup", menuName = "ScriptableObjects/CustomisationGroup")]
public class CustomisationGroup : ScriptableObject
{
	public CustomisationType ThisType;
	public bool CanColour;
	public List<PlayerCustomisationData> PlayerCustomisations = new List<PlayerCustomisationData>();
	public SpriteOrder SpriteOrder;



	//[NaughtyAttributes.Button()]
	// public void SetPlayerCustomisations()
	// {
	// 	var AllList = FindAssetsByType<PlayerCustomisationData>();
	// 	PlayerCustomisations = AllList.Where(x => x.Type == ThisType).ToList();
	// 	EditorUtility.SetDirty(this);
	// }
	//
	// public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
	// {
	// 	List<T> assets = new List<T>();
	// 	string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
	// 	for (int i = 0; i < guids.Length; i++)
	// 	{
	// 		string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
	// 		T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
	// 		if (asset != null)
	// 		{
	// 			assets.Add(asset);
	// 		}
	// 	}
	//
	// 	return assets;
	// }

}
