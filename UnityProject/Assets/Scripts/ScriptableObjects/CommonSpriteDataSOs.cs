using ScriptableObjects;
using UnityEngine;
[CreateAssetMenu(fileName = "CommonSpriteDataSOs", menuName = "Singleton/CommonSpriteDataSOs")]
public class CommonSpriteDataSOs : SingletonScriptableObject<CommonSpriteDataSOs>
{
	public SpriteDataSO bob;
	public SpriteDataSO outerwear;
	public SpriteDataSO belt;
	public SpriteDataSO head;
	public SpriteDataSO feet;
	public SpriteDataSO mask;
	public SpriteDataSO uniform;
	public SpriteDataSO leftHand;
	public SpriteDataSO rightHand;
	public SpriteDataSO eyes;
	public SpriteDataSO back;
	public SpriteDataSO hands;
	public SpriteDataSO ear;
	public SpriteDataSO neck;
	public SpriteDataSO handcuffs;
	public SpriteDataSO id;
	public SpriteDataSO suitStorage;

	// Inventory pockets (storage01 - storage20)
	public SpriteDataSO InventoryPocket;
}
