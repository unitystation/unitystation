using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class PipeRun : PipeItem
	{


		public int BentVariantLocation = 4;
		public int StraightVariantLocation = 0;
		public bool IsBent = false;

		public PipeTile StraightPipe;
		public PipeTile BentPipe;

		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Crowbar))
			{
				if (IsBent)//This assumes that the connections never get changed around/rotated
				{
					IsBent = false;
					SpriteHandler.ChangeSpriteVariant(StraightVariantLocation);
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"You straighten the pipe with the " + interaction.UsedObject.ExpensiveName());
					return;
				}
				else
				{
					IsBent = true;
					SpriteHandler.ChangeSpriteVariant(BentVariantLocation);
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"You Bend the pipe with the " + interaction.UsedObject.ExpensiveName());
					return;
				}

			}
			base.ServerPerformInteraction(interaction);
		}

		public override void Setsprite()
		{
			if (IsBent)//This assumes that the connections never get changed around/rotated
			{
				SpriteHandler.ChangeSpriteVariant(BentVariantLocation);

			}
			else
			{
				SpriteHandler.ChangeSpriteVariant(StraightVariantLocation);

			}
		}

		public override PipeTile GetPipeTile()
		{
			if (IsBent)
			{
				return (BentPipe);
			}
			else
			{
				return (StraightPipe);
			}
		}

	}
}