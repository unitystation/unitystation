
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Base class for the different types of cloth data.
/// </summary>
public abstract class BaseClothData : ScriptableObject
{
	public virtual List<Color> GetPaletteOrNull(int variantIndex)
	{
		return null;
	}
}
