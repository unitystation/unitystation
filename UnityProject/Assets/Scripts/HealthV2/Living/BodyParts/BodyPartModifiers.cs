using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
    /// Handles the body part's modifiers and efficiency
	public partial class BodyPart
	{
		/// <summary>
		/// This should be utilized in most implants so as to make changing the effectiveness of it easy.
		/// Some organs wont boil down to just one efficiency score, so you'll have to keep that in mind.
		/// </summary>
		[Tooltip("This is a generic variable representing the 'efficieny' of the implant." +
				 "Can be modified by implant modifiers.")]
		[SerializeField] protected float efficiency = 1;

		/// <summary>
		/// Event that fires when the body part's modifier total changes
		/// </summary>
		public event Action ModifierChange;

		/// <summary>
		/// The total product of all modifiers applied to this body part.  This acts as a multiplier for efficiency,
		/// thus a low TotalModified means the part is less effective, high means it is more effective
		/// </summary>
		[Tooltip("The total amount that modifiers are affecting this part's efficiency by")]
		public float TotalModified = 1;

		/// <summary>
		/// The list of all modifiers currently applied to this part
		/// </summary>
		[Tooltip("All modifiers applied to this")]
		public List<Modifier> AppliedModifiers = new List<Modifier>();

		/// <summary>
		/// Updates the body part's TotalModified value based off of the modifiers being applied to it
		/// </summary>
		public void UpdateMultiplier()
		{
			TotalModified = 1;
			foreach (var Modifier in AppliedModifiers)
			{
				TotalModified *= Mathf.Max(0, Modifier.Multiplier);
			}
			ModifierChange?.Invoke();
		}

		/// <summary>
		/// Adds a new modifier to the body part
		/// </summary>
		public void AddModifier(Modifier InModifier)
		{
			InModifier.RelatedPart = this;
			AppliedModifiers.Add(InModifier);
		}

		/// <summary>
		/// Removes a modifier from the bodypart
		/// </summary>
		public void RemoveModifier(Modifier InModifier)
		{
			InModifier.RelatedPart = null;
			AppliedModifiers.Remove(InModifier);
		}
    }

    /// <summary>
	/// A modifier that affects the efficiency of a body part.  Modifiers are applied multiplicatively
	/// </summary>
	public class Modifier
	{
		public float Multiplier
		{
			get
			{
				return multiplier;
			}
			set
			{
				if (multiplier != value)
				{
					multiplier = value;
					if (RelatedPart != null)
					{
						RelatedPart.UpdateMultiplier();
					}
				}
			}
		}

		private float multiplier;

		public BodyPart RelatedPart;
	}
}