using UnityEngine;

/// <summary>
/// Singleton, provides common ItemTraits with special purposes (such as components
/// used on many prefabs which automatically cause an object to have particular traits) so they can be easily used
/// in components without needing to be assigned in editor.
/// </summary>
[CreateAssetMenu(fileName = "CommonTraitsSingleton", menuName = "Singleton/Traits/CommonTraits")]
public class CommonTraits : SingletonScriptableObject<CommonTraits>
{
	public ItemTrait ReagentContainer;
	public ItemTrait CanisterFillable;
	public ItemTrait Gun;
	public ItemTrait Ingredient;
	public ItemTrait Food;
	public ItemTrait Mask;
	public ItemTrait Wirecutter;
	public ItemTrait Wrench;
	public ItemTrait Emag;
	public ItemTrait Crowbar;
	public ItemTrait Screwdriver;
	public ItemTrait NoSlip;
	public ItemTrait Slippery;
	public ItemTrait Multitool;
	public ItemTrait SpillOnThrow;
	public ItemTrait CanFillMop;
	public ItemTrait Cultivator;
	public ItemTrait Trowel;
	public ItemTrait Hatchet;
	public ItemTrait Bucket;
	public ItemTrait MetalSheet;
	public ItemTrait GlassSheet;
	public ItemTrait PlasteelSheet;
	public ItemTrait ReinforcedGlassSheet;
	public ItemTrait WoodenPlank;
	public ItemTrait Cable;
	public ItemTrait Egg;
	public ItemTrait Welder;
	public ItemTrait Shovel;
	public ItemTrait Knife;
	public ItemTrait RollingPin;
	public ItemTrait Transforamble;
	public ItemTrait Squeaky;
	public ItemTrait LightTube;
	public ItemTrait LightBulb;
	public ItemTrait LightReplacer;
	public ItemTrait BlackGloves;
	public ItemTrait Broken;
	public ItemTrait Breakable;
	public ItemTrait Rods;
	public ItemTrait SolidPlasma;
	public ItemTrait NukeDisk;
	public ItemTrait Insulated;
	public ItemTrait BudgetInsulated;
	public ItemTrait InternalBattery;
	public ItemTrait AntiFacehugger;
	public ItemTrait PickUpProtection;
	public ItemTrait ReactorRod;
	public ItemTrait Pickaxe;
	public ItemTrait CanPryDoor;
}