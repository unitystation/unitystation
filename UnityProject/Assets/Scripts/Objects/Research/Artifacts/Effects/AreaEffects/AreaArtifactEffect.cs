using System.Linq;
using UnityEngine;

namespace Systems.Research
{
	public class AreaArtifactEffect : ArtifactEffect
	{
		public EffectShapeType effectShapeType = EffectShapeType.Square;

		[Tooltip("How far this artifact will effect players from")]
		public int AuraRadius = 10;

		[Tooltip("Wearing this trait protects the user from the area effect")]
		public ItemTrait resistanceTrait = null;

		[Tooltip("Allows the artifact to effect the head and body differently if they are wearing only partially protective gear")]
		public bool AllowBodySplitting = false;

		public virtual void DoEffectAura(GameObject centeredAround)
		{
			var objCenter = centeredAround.AssumedWorldPosServer().RoundToInt();
			var shape = EffectShape.CreateEffectShape(effectShapeType, objCenter, AuraRadius);

			foreach (var pos in shape)
			{
				// check if tile has any alive player
				var players = MatrixManager.GetAt<PlayerScript>(pos, true).Distinct();
				foreach (var player in players)
				{
					if (!player.IsDeadOrGhost)
					{
						OnEffect(player);
					}
				}
			}
		}

		public virtual void OnEffect(PlayerScript player)
		{
			//Area effect here
		}
	}
}
