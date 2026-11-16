using UnityEngine;
using US13.Clothing;
using US13.HealthV2.Living.BodyParts;

namespace US13.Mobs.BrainAI.States.SimpleBot
{
	/// <summary>
	/// Client presentation only. BrainMobAI supplies the replicated task status.
	/// </summary>
	public class SimpleBotBody : MonoBehaviour
	{
		[SerializeField, Tooltip("Regular idle, regular working, emagged idle, emagged working.")]
		private SpriteDataSO[] possibleSprites = new SpriteDataSO[4];
		private BodyPart part;
		private BrainMobAI previousBrain;
		private int previousIndex = -1;
		private void Awake() => part = GetComponentInParent<BodyPart>(true);
		private void OnEnable() { previousBrain = null; previousIndex = -1; }
		private void LateUpdate()
		{
			var brain = part?.HealthMaster?.brain?.GetBodyPartBehavior<BrainMobAI>();
			if (brain == null) { previousBrain = null; previousIndex = -1; return; }
			int index = (brain.IsEmagged ? 2 : 0) + (brain.IsPerformingTask ? 1 : 0);
			if (previousBrain == brain && previousIndex == index) return;
			if (part.RelatedPresentSprites.Count == 0 || index >= possibleSprites.Length || possibleSprites[index] == null) return;
			part.RelatedPresentSprites[0].UpdateSpritesForImplant(part, ClothingHideFlags.HIDE_NONE, possibleSprites[index], part.SpritePrefab.SpriteOrder);
			previousBrain = brain;
			previousIndex = index;
		}
	}
}
