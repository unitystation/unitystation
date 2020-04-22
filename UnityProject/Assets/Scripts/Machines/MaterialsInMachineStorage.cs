using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialsInMachineStorage", menuName = "ScriptableObjects/Machines/MaterialsInMachineStorage")]
public class MaterialsInMachineStorage : ScriptableObject
{
	[Tooltip("The materials used in machines such as exosuit fabricator, protolathe and so on")]
	public List<MaterialSheet> materials;

	[Tooltip("2000cm per sheet is standard for SS13")]
	public int cm3PerSheet;
}