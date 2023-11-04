using System;
using System.Collections.Generic;
using NaughtyAttributes;
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
	[Range(0, 100)] public float Anomaly;

	[Range(0,100)] public int DismembermentProtectionChance;

	public bool StunImmunity = false;


	[MinMaxSlider(-5000.00f, 50000.0f)]
	public Vector2 TemperatureProtectionInK = new Vector2( 268.15f, 313.15f);

	[MinMaxSlider(-5000.00f, 50000.0f)]
	public Vector2 PressureProtectionInKpa = new Vector2(30f, 300f);


	/// <summary>
	/// Calculates how much damage would be done based on armor resistance and armor penetration.
	/// </summary>
	/// <param name="damage">Base damage</param>
	/// <param name="attackType">Type of attack</param>
	/// <param name="armorPenetration">How well the attack will break through different types of armor</param>
	/// <returns>New damage after applying protection values</returns>
	public float GetDamage(float damage, AttackType attackType, float armorPenetration = 0)
	{
		return damage * GetRatingValue(attackType, armorPenetration);
	}


	/// <summary>
	/// From the damage done, calculates how much force was put into it
	/// </summary>
	/// <param name="damage">Base damage</param>
	/// <param name="attackType">Type of attack</param>
	/// <param name="armorPenetration">How well the attack will break through different types of armor</param>
	/// <returns>New damage after applying protection values</returns>
	public float GetForce(float damage, AttackType attackType, float armorPenetration = 0)
	{
		return damage / GetRatingValue(attackType, armorPenetration);
	}

	/// <summary>
	/// Get the proportion of damage that will be dealt through this armor
	/// depending on the armor penetration of the attack.
	/// </summary>
	/// <param name="attackType">Type of attack</param>
	/// <param name="armorPenetration">How well or poorly the attack will break through different types of armor</param>
	/// <returns>What proportion of damage will be dealt through this armor</returns>
	public float GetRatingValue(AttackType attackType, float armorPenetration = 0)
	{
		return  1 - GetRating(attackType, armorPenetration) / 100;
	}

	/// <summary>
	/// Get the armor protection rating from the attackType depending on armor penetration of the attack.
	/// </summary>
	/// <param name="attackType">Type of attack</param>
	/// <param name="armorPenetration">How well the attack will break through different types of armor</param>
	/// <returns>The armor protection rating from the attackType depending on armor penetration of the attack</returns>
	public float GetRating(AttackType attackType, float armorPenetration)
	{
		float armorRating = GetRating(attackType);
		return armorRating < 0 ? armorRating : armorRating * (1 - armorPenetration / 100);
	}

	/// <summary>
	/// Calculates how much damage would be done based on multiple armors' resistance
	/// and armor penetration of the attack.
	/// </summary>
	/// <param name="damage">Base damage</param>
	/// <param name="attackType">Type of attack</param>
	/// <param name="armors">List of armor trying to protect something from damage</param>
	/// <param name="armorPenetration">Armor penetration of the attack</param>
	/// <returns>New damage after applying protection and armor penetration values</returns>
	public static float GetTotalDamage(
		float damage,
		AttackType attackType,
		IEnumerable<Armor> armors,
		float armorPenetration = 0
	)
	{
		foreach (Armor armor in armors)
		{
			damage *= armor.GetRatingValue(attackType, armorPenetration);
		}

		return damage;
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


	public bool InvalidValuesInTemperature() //Due to crappy unity serialisation
	{
		if ( TemperatureProtectionInK.y == 0)
		{
			return true;
		}

		return false;
	}

	public bool InvalidValuesInPressure() //Due to crappy unity serialisation
	{
		if (PressureProtectionInKpa.y == 0)
		{
			return true;
		}

		return false;
	}

	public float GetMiddleTemperature()
	{
		return (TemperatureProtectionInK.x + TemperatureProtectionInK.y) / 2f;
	}
	public float GetMiddlePressure()
	{
		return (PressureProtectionInKpa.x + PressureProtectionInKpa.y) / 2f;
	}

	public bool TemperatureOutsideSafeRange(float temperature)
	{
		if (InvalidValuesInTemperature())
		{
			return false;
		}

		if (temperature < TemperatureProtectionInK.x || temperature > TemperatureProtectionInK.y)
		{
			return true;
		}

		return false;
	}


	public bool PressureOutsideSafeRange(float pressure)
	{
		if (InvalidValuesInPressure())
		{
			return false;
		}


		if (pressure < PressureProtectionInKpa.x || pressure > PressureProtectionInKpa.y)
		{
			return true;
		}

		return false;
	}

	public bool PressureNearingLimits(float pressure, out bool highPressure)
	{
		highPressure = false;
		if (InvalidValuesInPressure())
		{
			return false;
		}

		var mid = GetMiddlePressure();

		var AlertMin =  mid + (0.75f * (PressureProtectionInKpa.x - mid));
		var AlertMax =  mid + (0.75f * ( PressureProtectionInKpa.y - mid));



		if (pressure < (AlertMin))
		{
			highPressure = false;
			return true;
		}
		else if (pressure > AlertMax)
		{
			highPressure = true;
			return true;
		}

		return false;
	}

	public bool TemperatureNearingLimits(float temperature, out bool highTemperature)
	{
		highTemperature = false;
		if (InvalidValuesInPressure())
		{
			return false;
		}


		var mid = GetMiddleTemperature();

		var AlertMin =  mid + (0.75f * (TemperatureProtectionInK.x - mid));
		var AlertMax =  mid + (0.75f * ( TemperatureProtectionInK.y - mid));


		if (temperature < (AlertMin))
		{
			highTemperature = false;
			return true;
		}
		else if (temperature > AlertMax)
		{
			highTemperature = true;
			return true;
		}


		return false;
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
