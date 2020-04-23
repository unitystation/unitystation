using System;
using UnityEngine;

/// <summary>
/// Just so the guns can find there ammo;
/// TODO Should be replaced with slots and Slot populaters
/// </summary>
[CreateAssetMenu(fileName = "AmmoPrefabsSingleton", menuName = "Singleton/AmmoPrefabs")]
public class AmmoPrefabs : SingletonScriptableObject<AmmoPrefabs>
{
	[SerializeField]
	private GameObject _9mm = null;
	[SerializeField]
	private GameObject uzi9mm = null;
	[SerializeField]
	private GameObject smg9mm = null;
	[SerializeField]
	private GameObject tommy9mm = null;
	[SerializeField]
	private GameObject _10mm = null;
	[SerializeField]
	private GameObject _46mm = null;
	[SerializeField]
	private GameObject _50mm = null;
	[SerializeField]
	private GameObject _556mm = null;
	[SerializeField]
	private GameObject _38 = null;
	[SerializeField]
	private GameObject _45 = null;
	[SerializeField]
	private GameObject _50 = null;
	[SerializeField]
	private GameObject _357 = null;
	[SerializeField]
	private GameObject _762 = null;
	[SerializeField]
	private GameObject A762 = null;
	[SerializeField]
	private GameObject FusionCells = null;
	[SerializeField]
	private GameObject Slug = null;
	[SerializeField]
	private GameObject Syringe = null;
	[SerializeField]
	private GameObject Gasoline = null;
	[SerializeField]
	private GameObject Internal = null;

	/// <summary>
	/// Get the prefab of the ammo type so you can
	/// use it for spawning
	/// </summary>
	public static GameObject GetAmmoPrefab(AmmoType ammoType)
	{
		switch (ammoType)
		{
			case AmmoType._9mm:
				return Instance._9mm;
			case AmmoType.uzi9mm:
				return Instance.uzi9mm;
			case AmmoType.smg9mm:
				return Instance.smg9mm;
			case AmmoType.tommy9mm:
				return Instance.tommy9mm;
			case AmmoType._10mm:
				return Instance._10mm;
			case AmmoType._46mm:
				return Instance._46mm;
			case AmmoType._50mm:
				return Instance._50mm;
			case AmmoType._556mm:
				return Instance._556mm;
			case AmmoType._38:
				return Instance._38;
			case AmmoType._45:
				return Instance._45;
			case AmmoType._50:
				return Instance._50;
			case AmmoType._357:
				return Instance._357;
			case AmmoType._762:
				return Instance._762;
			case AmmoType.A762:
				return Instance.A762;
			case AmmoType.FusionCells:
				return Instance.FusionCells;
			case AmmoType.Slug:
				return Instance.Slug;
			case AmmoType.Syringe:
				return Instance.Syringe;
			case AmmoType.Gasoline:
				return Instance.Gasoline;
			case AmmoType.Internal:
				return Instance.Internal;
			default:
				throw new ArgumentOutOfRangeException(nameof(ammoType), ammoType, null);
		}
	}
}
