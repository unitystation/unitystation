using System.Collections.Generic;
using NaughtyAttributes;
using Systems.Spawns;
using UnityEngine;

namespace ScriptableObjects.Characters
{
	[CreateAssetMenu(fileName = "CharacterAttribute", menuName = "CharacterAttribute")]
	public class CharacterAttribute : ScriptableObject
	{
		[Tooltip("Name of the attribute that will appear in things like the admin tools, character quirks, job selection, etc")]
		public string DisplayName = "Unknown";

		[Tooltip("Description of this attribute")]
		public string Description = "Void of identity";

		public Sprite Icon;

		[Tooltip("Can two of this same attribute exist on one character?")]
		public bool CanHaveTwoOfThis = false;

		[Tooltip("What other attributes that cannot coexist with this one?")]
		public List<CharacterAttribute> BlackListedAttributes = new List<CharacterAttribute>();

		[Tooltip("Behaviors that spawn when the attribute gets added. (Populating inventory, giving items/spells, gaining sickness, etc")]
		public List<GameObject> OnAddBehaviors = new List<GameObject>();

		[Tooltip("The color pallet of this attribute, mainly used in UI but can be used elsewhere for various different reasons and use cases.")]
		public List<Color> AttributeColorPallet = new List<Color>();
	}
}