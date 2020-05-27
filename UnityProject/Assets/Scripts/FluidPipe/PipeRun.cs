using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class PipeRun : PipeItem
	{


		public int BentVariantLocation = 4;
		public int StraightVariantLocation = 0;


		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Crowbar))
			{
				if (Connections.Directions[(int)PipeDirection.East].Bool)//This assumes that the connections never get changed around/rotated
				{
					var tmp = Connections.Directions[(int) PipeDirection.East];

					Connections.Directions[(int)PipeDirection.East]  =
						Connections.Directions[(int) PipeDirection.North];

					Connections.Directions[(int) PipeDirection.North] = tmp;

					SpriteHandler.ChangeSpriteVariant(StraightVariantLocation);
					Chat.AddExamineMsgFromServer(interaction.Performer,
						"You straighten the pipe with the " + interaction.UsedObject.ExpensiveName());
					return;
				}
				else
				{
					var tmp = Connections.Directions[(int) PipeDirection.North];

					Connections.Directions[(int) PipeDirection.North] =
						Connections.Directions[(int) PipeDirection.East];

					Connections.Directions[(int)PipeDirection.East] = tmp;

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
			if (Connections.Directions[(int)PipeDirection.East].Bool)//This assumes that the connections never get changed around/rotated
			{
				SpriteHandler.ChangeSpriteVariant(BentVariantLocation);

			}
			else
			{
				SpriteHandler.ChangeSpriteVariant(StraightVariantLocation);

			}
		}

	}
}