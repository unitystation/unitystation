using UnityEngine;

/// <summary>
/// Just so the guns can find there ammo;
/// TODO Should be replaced with slots and Slot populaters
/// </summary>
[CreateAssetMenu(fileName = "AmmoPrefabsSingleton", menuName = "Singleton/AmmoPrefabs")]
public class AmmoPrefabs : SingletonScriptableObject<AmmoPrefabs>
{
	[SerializeField]
	private GameObject _12mm;
	[SerializeField]
	private GameObject _5Point56mm;
	[SerializeField]
	private GameObject _9mm;
	[SerializeField]
	private GameObject _38;
	[SerializeField]
	private GameObject _46x30mmtT;
	[SerializeField]
	private GameObject _50mm;
	[SerializeField]
	private GameObject _357mm;
	[SerializeField]
	private GameObject A762;
	[SerializeField]
	private GameObject FusionCells;
	[SerializeField]
	private GameObject Slug;
	[SerializeField]
	private GameObject smg9mm;
	[SerializeField]
	private GameObject Syringe;
	[SerializeField]
	private GameObject uzi9mm;
	[SerializeField]
	private GameObject Internal;

	/// <summary>
	/// Get the prefab of the ammo type so you can
	/// use it for spawning
	/// </summary>
	public static GameObject GetAmmoPrefab(AmmoType ammoType)
	{
		switch (ammoType)
		{
			case AmmoType.A762:
				return Instance.A762;
			case AmmoType.FusionCells:
				return Instance.FusionCells;
			case AmmoType.Slug:
				return Instance.Slug;
			case AmmoType.smg9mm:
				return Instance.smg9mm;
			case AmmoType.Syringe:
				return Instance.Syringe;
			case AmmoType.uzi9mm:
				return Instance.uzi9mm;
			case AmmoType._12mm:
				return Instance._12mm;
			case AmmoType._357mm:
				return Instance._357mm;
			case AmmoType._38:
				return Instance._38;
			case AmmoType._46x30mmtT:
				return Instance._46x30mmtT;
			case AmmoType._50mm:
				return Instance._50mm;
			case AmmoType._5Point56mm:
				return Instance._5Point56mm;
			case AmmoType._9mm:
				return Instance._9mm;
			case AmmoType.Internal:
				return Instance.Internal;
		}
		return null;
	}
}
