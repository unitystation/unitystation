
using UnityEngine;

/// <summary>
/// Singleton, provides common prefabs with special purposes so they can be easily used
/// in components without needing to be assigned in editor.
/// </summary>
[CreateAssetMenu(fileName = "CommonPrefabsSingleton", menuName = "Singleton/CommonPrefabs")]
public class CommonPrefabs : SingletonScriptableObject<CommonPrefabs>
{

	public GameObject Metal;
	public GameObject Plasteel;
	public GameObject GlassSheet;
	public GameObject WoodenPlank;
	public GameObject SingleCableCoil;
	public GameObject Mask;
	public GameObject EmergencyOxygenTank;

}
