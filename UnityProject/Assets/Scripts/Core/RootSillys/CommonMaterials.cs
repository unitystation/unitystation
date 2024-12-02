using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

[CreateAssetMenu(fileName = "CommonMaterials", menuName = "Singleton/CommonMaterials")]
public class CommonMaterials : BadSingletonScriptableObject<CommonMaterials>
{
	public Material DefaultLightMaterial;

}
