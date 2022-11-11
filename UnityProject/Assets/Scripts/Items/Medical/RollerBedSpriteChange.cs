using UnityEngine;
using Objects;

namespace Items.Medical
{
	///<summary>
	/// Class which subscribes to the OnBuckle Event in BuckleInteract to enable the changing of sprites when someone 
	/// is buckled and unbuckled from the bed. 
	///</summary>

	public class RollerBedSpriteChange : MonoBehaviour
	{
		[SerializeField] private BuckleInteract buckleInteract;
		[SerializeField] private SpriteHandler spriteHandler;
		private const int ROLLER_BED_SPRITE_INDEX_DOWN = 0;
		private const int ROLLER_BED_SPRITE_INDEX_UP = 1;


		private void OnEnable()
		{
			buckleInteract.OnBuckleEvent += OnBuckle;
			buckleInteract.OnUnbuckleEvent += OnUnbuckle;
		}

		private void OnDisable()
		{
			buckleInteract.OnBuckleEvent -= OnBuckle;
			buckleInteract.OnUnbuckleEvent -= OnUnbuckle;
		}
		private void OnBuckle()
		{
			spriteHandler.ChangeSprite(ROLLER_BED_SPRITE_INDEX_UP);
		}

		private void OnUnbuckle()
		{
			spriteHandler.ChangeSprite(ROLLER_BED_SPRITE_INDEX_DOWN);
		}
	}
}