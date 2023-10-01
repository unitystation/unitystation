using System;
using System.Collections.Generic;
using NaughtyAttributes;
using ScriptableObjects;
using UnityEngine;

/// <summary>
/// Singleton, provides common ItemTraits with special purposes (such as components
/// used on many prefabs which automatically cause an object to have particular traits) so they can be easily used
/// in components without needing to be assigned in editor.
/// </summary>
[CreateAssetMenu(fileName = "CommonTraitsSingleton", menuName = "Singleton/Traits/CommonTraits")]
public class CommonTraits : SingletonScriptableObject<CommonTraits>
{
	[BoxGroup("Guns")] public ItemTrait Gun;
	[BoxGroup("Guns")] public ItemTrait Suppressor;
	[BoxGroup("Guns")] public ItemTrait WeaponCell;
	[BoxGroup("Guns")] public ItemTrait FiringPin;

	[BoxGroup("Food and related")] public ItemTrait Ingredient;
	[BoxGroup("Food and related")] public ItemTrait Food;
	[BoxGroup("Food and related")] public ItemTrait Cheese;
	[BoxGroup("Food and related")] public ItemTrait Pizza;
	[BoxGroup("Food and related")] public ItemTrait Seeds;
	[BoxGroup("Food and related")] public ItemTrait Trash;
	[BoxGroup("Food and related")] public ItemTrait Wheat;
	[BoxGroup("Food and related")] public ItemTrait Egg;

	[BoxGroup("Tools")] public ItemTrait Wirecutter;
	[BoxGroup("Tools")] public ItemTrait Wrench;
	[BoxGroup("Tools")] public ItemTrait Crowbar;
	[BoxGroup("Tools")] public ItemTrait Screwdriver;
	[BoxGroup("Tools")] public ItemTrait Multitool;
	[BoxGroup("Tools")] public ItemTrait Hatchet;
	[BoxGroup("Tools")] public ItemTrait Cultivator;
	[BoxGroup("Tools")] public ItemTrait Trowel;
	[BoxGroup("Tools")] public ItemTrait Bucket;
	[BoxGroup("Tools")] public ItemTrait Cable;
	[BoxGroup("Tools")] public ItemTrait Welder;
	[BoxGroup("Tools")] public ItemTrait Shovel;
	[BoxGroup("Tools")] public ItemTrait Knife;
	[BoxGroup("Tools")] public ItemTrait Emag;
	[BoxGroup("Tools")] public ItemTrait ReagentContainer;
	[BoxGroup("Tools")] public ItemTrait RollingPin;
	[BoxGroup("Tools")] public ItemTrait LightReplacer;
	[BoxGroup("Tools")] public ItemTrait Pickaxe;
	[BoxGroup("Tools")] public ItemTrait ScienceScan;
	[BoxGroup("Tools")] public ItemTrait AirlockPainter;

	[BoxGroup("Surgical")] public ItemTrait Scalpel;
	[BoxGroup("Surgical")] public ItemTrait Retractor;
	[BoxGroup("Surgical")] public ItemTrait CircularSaw;
	[BoxGroup("Surgical")] public ItemTrait Hemostat;
	[BoxGroup("Surgical")] public ItemTrait Cautery;

	[BoxGroup("Characteristics")] public ItemTrait NoSlip;
	[BoxGroup("Characteristics")] public ItemTrait Slippery;
	[BoxGroup("Characteristics")] public ItemTrait SpillOnThrow;
	[BoxGroup("Characteristics")] public ItemTrait CanFillMop;
	[BoxGroup("Characteristics")] public ItemTrait Squeaky;
	[BoxGroup("Characteristics")] public ItemTrait Transforamble;
	[BoxGroup("Characteristics")] public ItemTrait Broken;
	[BoxGroup("Characteristics")] public ItemTrait Insulated;
	[BoxGroup("Characteristics")] public ItemTrait BudgetInsulated;
	[BoxGroup("Characteristics")] public ItemTrait AntiFacehugger;
	[BoxGroup("Characteristics")] public ItemTrait PickUpProtection;
	[BoxGroup("Characteristics")] public ItemTrait CanPryDoor;
	[BoxGroup("Characteristics")] public ItemTrait Loomable;
	[BoxGroup("Characteristics")] public ItemTrait CanisterFillable;
	[BoxGroup("Characteristics")] public ItemTrait Breakable;
	[BoxGroup("Characteristics")] public ItemTrait EMPResistant;
	[BoxGroup("Characteristics")] public ItemTrait Gag;

	[BoxGroup("Materials")] public ItemTrait OreGeneral;
	[BoxGroup("Materials")] public ItemTrait MetalSheet;
	[BoxGroup("Materials")] public ItemTrait GlassSheet;
	[BoxGroup("Materials")] public ItemTrait PlasteelSheet;
	[BoxGroup("Materials")] public ItemTrait ReinforcedGlassSheet;
	[BoxGroup("Materials")] public ItemTrait WoodenPlank;
	[BoxGroup("Materials")] public ItemTrait Rods;
	[BoxGroup("Materials")] public ItemTrait SolidPlasma;
	[BoxGroup("Materials")] public ItemTrait OrePlasma;
	[BoxGroup("Materials")] public ItemTrait DiamondSheet;

	[BoxGroup("Clothing")] public ItemTrait Mask;
	[BoxGroup("Clothing")] public ItemTrait GasMask;
	[BoxGroup("Clothing")] public ItemTrait BlackGloves;
	[BoxGroup("Clothing")] public ItemTrait WizardGarb;
	[BoxGroup("Clothing")] public ItemTrait Sunglasses;

	public ItemTrait LightTube;
	public ItemTrait LightBulb;
	public ItemTrait NukeDisk;
	public ItemTrait InternalBattery;
	public ItemTrait ReactorRod;
	public ItemTrait RawCottonBundle;
	public ItemTrait RawDurathreadBundle;
	public ItemTrait BluespaceActivity;
	public ItemTrait Id;
	public ItemTrait ProximitySensor;
	public ItemTrait PowerControlBoard;
	public ItemTrait Beaker;

	public ItemTrait CoreBodyPart;

	public ItemTrait Pill;

	/// <summary>
	/// Do not use this list to get references to traits, locally reference them in your scripts instead!
	/// </summary>
	[Obsolete]
	public List<ItemTrait> everyTraitOutThere = new List<ItemTrait>();
}
