using System;
using UnityEngine;

/// <summary>
/// Represents armor which provides resistance against various kinds of attacks.
///
/// an armor rating of 100 indicates complete resistance against that kind of attack.
/// 0 indicates no resistance.
/// negative values indicate weakness to that kind of attack.
///
/// Each value represents a protection percentage, should be between 0 and 100.
/// </summary>
[Serializable]
public class Armor
{
	[Range(-100,100)] public float Melee;
	[Range(-100,100)] public float Bullet;
	[Range(-100,100)] public float Laser;
	[Range(-100,100)] public float Energy;
	[Range(-100,100)] public float Bomb;
	[Range(-100,100)] public float Rad;
	[Range(-100,100)] public float Fire;
	[Range(-100,100)] public float Acid;
	[Range(-100,100)] public float Magic;
	[Range(-100,100)] public float Bio;

	/// <summary>
	/// Calculates how much damage would be done based on armor resistance
	/// </summary>
	/// <param name="damage">base damage</param>
	/// <param name="attackType">type of attack</param>
	/// <returns>new damage after applying protection values</returns>
	public float GetDamage(float damage, AttackType attackType)
	{
		return damage * GetRatingValue(attackType);
	}

	public float GetRatingValue(AttackType attackType)
	{
		float rating = GetRating(attackType);
		if (rating > 100)
		{
			rating = 100;
		}
		return  (1 - rating / 100);
	}


	/// <summary>
	/// Get the armor rating against a certain type of attack
	/// </summary>
	/// <param name="attackType"></param>
	/// <returns>a value no greater than 100, indicating how much protection
	/// the armor provides against this kind of attack</returns>
	public float GetRating(AttackType attackType)
	{
		switch (attackType)
		{
			case AttackType.Melee:
				return Melee;
			case AttackType.Bullet:
				return Bullet;
			case AttackType.Laser:
				return Laser;
			case AttackType.Energy:
				return Energy;
			case AttackType.Bomb:
				return Bomb;
			case AttackType.Bio:
				return Bio;
			case AttackType.Rad:
				return Rad;
			case AttackType.Fire:
				return Fire;
			case AttackType.Acid:
				return Acid;
			case AttackType.Magic:
				return Magic;

		}

		return 0;
	}

	/// <summary>
	/// Operator override to add all armor types with ease. Thank you, Redline.
	/// </summary>
	public static Armor operator +(Armor a, Armor b)
	{
		var armor = new Armor
		{
			Melee = a.Melee + b.Melee,
			Bullet = a.Bullet + b.Bullet,
			Laser = a.Laser + b.Laser,
			Energy = a.Energy + b.Energy,
			Bomb = a.Bomb + b.Bomb,
			Rad = a.Rad + b.Rad,
			Fire = a.Fire + b.Fire,
			Acid = a.Acid + b.Acid,
			Magic = a.Magic + b.Magic,
			Bio = a.Bio + b.Bio
		};

		return armor;
	}

	/// <summary>
	/// Operator override to subtract all armor types with ease. Thank you, Redline.
	/// </summary>
	public static Armor operator -(Armor a, Armor b)
	{
		var armor = new Armor
		{
			Melee = a.Melee - b.Melee,
			Bullet = a.Bullet - b.Bullet,
			Laser = a.Laser - b.Laser,
			Energy = a.Energy - b.Energy,
			Bomb = a.Bomb - b.Bomb,
			Rad = a.Rad - b.Rad,
			Fire = a.Fire - b.Fire,
			Acid = a.Acid - b.Acid,
			Magic = a.Magic - b.Magic,
			Bio = a.Bio - b.Bio
		};

		return armor;
	}
}

/// <summary>
/// A type of attack - not quite the same as a type of damage. A type of damage
/// is something that is applied to an object. A type of attack is a way of applying
/// damage to an object.
/// </summary>
public enum AttackType
{
	Melee = 0,
	Bullet = 1,
	Laser = 2,
	Energy = 3,
	Bomb = 4,
	Rad = 5,
	Fire = 6,
	Acid = 7,
	Magic = 8,
	Bio = 9,
	///type of attack that bypasses armor - such as suffocating. It's not possible
	/// to have armor against this
	Internal = 10
}
