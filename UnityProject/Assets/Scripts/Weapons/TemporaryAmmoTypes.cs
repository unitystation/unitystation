using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Just so the guns can find there ammo; 
/// TODO Should be replaced with slots and Slot populaters
/// </summary>
[CreateAssetMenu(fileName = "TemporaryAmmoTypesSingleton", menuName = "Singleton/TemporaryAmmoTypes")]
public class TemporaryAmmoTypes : SingletonScriptableObject<TemporaryAmmoTypes>
{
	public GameObject _12mm;
	public GameObject _5Point56mm;
	public GameObject _9mm;
	public GameObject _38;
	public GameObject _46x30mmtT;
	public GameObject _50mm;
	public GameObject _357mm;
	public GameObject A762;
	public GameObject FusionCells;
	public GameObject Slug;
	public GameObject smg9mm;
	public GameObject Syringe;
	public GameObject uzi9mm;
}
