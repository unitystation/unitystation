using System;
using UnityEngine;

/// <summary>
/// Destroys gameobject whenever this gets disabled
/// </summary>
public class DestroyOnDisable : MonoBehaviour
{
	private void OnDisable()
	{
		Destroy(gameObject);
	}
}