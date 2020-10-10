using AddressableReferences;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FloorTileTypes
{
    public FloorTileType FloorTileType;
    public List<AddressableAudioSource> AddressableAudioSources = new List<AddressableAudioSource>();
}

[Serializable]
public class StepTypes
{
    public StepType StepType;
    public List<FloorTileTypes> FloorTileTypes = new List<FloorTileTypes>();
}

/// <summary>
/// Allows to assign a list of AddressableAudioSource to a list of FloorTileType and a list of StepTypes
/// </summary>
[CreateAssetMenu(fileName = "StepSound", menuName = "ScriptableObjects/StepSound")]
public class StepSounds : ScriptableObject
{
    public List<StepTypes> StepTypes = new List<StepTypes>();
}
