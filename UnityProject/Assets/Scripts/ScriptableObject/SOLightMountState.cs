using Items;
using Microsoft.CSharp.RuntimeBinder;
using UnityEngine;
public enum LightMountState
{
	On,
	Off,
	MissingBulb,
	Broken,
	Emergency,
	BurnedOut
}
[CreateAssetMenu(fileName = "SOLightMountState", menuName = "ScriptableObjects/States/SOLightMountState", order = 0)]
public class SOLightMountState : UnityEngine.ScriptableObject
{
	[SerializeField]private SpritesDirectional spritesDirectional;
	public SpritesDirectional SpritesDirectional => spritesDirectional;

	[SerializeField]private LightMountState state;
	public LightMountState State => state;

	[Tooltip("Will drop this item.")]
	[SerializeField]private GameObject tube;
	public GameObject Tube => tube;

	[Tooltip("Item with this trait will be put in.")]
	[SerializeField]private ItemTrait traitRequired;
	public ItemTrait TraitRequired => traitRequired;

	[Tooltip("On what % of integrity mount changes state.")]
	[Range(0.1f, 0.90f)]
	[SerializeField]private float multiplierIntegrity;
	public float MultiplierIntegrity => multiplierIntegrity;

	[Tooltip("Drops this on destroy.")]
	[SerializeField]private RandomItemPool lootDrop;
	public RandomItemPool LootDrop => lootDrop;

	[Tooltip("Light color.")]
	[SerializeField]private Color lightColor;
	public Color LightColor => lightColor;
}
