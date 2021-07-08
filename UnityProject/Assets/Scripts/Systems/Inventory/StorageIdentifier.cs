using System;
using UnityEngine;

/// <summary>
/// Used to identify storage items
/// </summary>
public class StorageIdentifier : MonoBehaviour
{
	public StorageItemName StorageItemName;
}

[Flags]
public enum StorageItemName
{
	CardboardBox = 1 << 0,
	ToolBox = 1 << 1,
	MedKit = 1 << 2,
	Backpack = 1 << 3,
	Dufflebag = 1 << 4,
	Belt = 1 << 5,
	Briefcase = 1 << 6,
	FirefirstAidKit = 1 << 7,
	Satchel = 1 << 8
}