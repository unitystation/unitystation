using Items;
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
	public SpritesDirectional spritesDirectional;

	public LightMountState state;

	[Tooltip("Will drop this item.")]
	public GameObject Tube;

	[Tooltip("Item with this trait will be put in.")]
	public ItemTrait traitRequired;

	[Tooltip("On what % of integrity mount changes state.")]
	[Range(0.1f, 0.90f)]
	public float multiplierBroken;

	[Tooltip("Drops this on destroy.")]
	public RandomItemPool lootDrop;

	[Tooltip("Light color.")]
	public Color lightState;
}
