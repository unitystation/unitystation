using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;

namespace Pipes
{
	public class Mixer : MonoPipe
	{
		public SpriteHandler spriteHandlerOverlay = null;

		private MixAndVolume IntermediateMixAndVolume = new MixAndVolume();

		public int MaxPressure = 10000;
		private float TransferMoles = 500f;

		public float ToTakeFromInputOne = 0.5f;
		public float ToTakeFromInputTwo = 0.5f;

		public bool IsOn = false;


		public override void Start()
		{
			pipeData.PipeAction = new MonoActions();
			spriteHandlerOverlay.PushClear();
			base.Start();
		}

		public void TogglePower()
		{
			IsOn = !IsOn;
			if (IsOn)
			{
				spriteHandlerOverlay.PushTexture();
			}
			else
			{
				spriteHandlerOverlay.PushClear();
			}
		}

		public override void Interaction(HandApply interaction)
		{
			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.Mixer, TabAction.Open);
		}

		public override void TickUpdate()
		{
			if (IsOn == false)
			{
				return;
			}

			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			if (pipeData.mixAndVolume.Density().y > MaxPressure || pipeData.mixAndVolume.Density().x > MaxPressure)
			{
				return;
			}

			var InputOne = pipeData.Connections.GetFlagToDirection(FlagLogic.InputOne)?.Connected;
			var InputTwo = pipeData.Connections.GetFlagToDirection(FlagLogic.InputTwo)?.Connected;

			if (InputOne == null || InputTwo == null) return;

			if (ToTakeFromInputOne == 1)
			{
				Vector2 Toremove = new Vector2(TransferMoles, TransferMoles);
				;
				if (Toremove.y > InputOne.GetMixAndVolume.Total.y)
				{
					Toremove.y = InputOne.GetMixAndVolume.Total.y;
				}

				if (Toremove.x > InputOne.GetMixAndVolume.Total.x)
				{
					Toremove.x = InputOne.GetMixAndVolume.Total.x;
				}

				InputOne.GetMixAndVolume.TransferTo(pipeData.mixAndVolume, Toremove);
				return;
			}

			if (ToTakeFromInputTwo == 1)
			{
				Vector2 Toremove = new Vector2(TransferMoles, TransferMoles);
				;
				if (Toremove.y > InputTwo.GetMixAndVolume.Total.y)
				{
					Toremove.y = InputTwo.GetMixAndVolume.Total.y;
				}

				if (Toremove.x > InputTwo.GetMixAndVolume.Total.x)
				{
					Toremove.x = InputTwo.GetMixAndVolume.Total.x;
				}

				InputTwo.GetMixAndVolume.TransferTo(pipeData.mixAndVolume, Toremove);
				return;
			}

			var Totalone = InputOne.GetMixAndVolume.Total;
			var TotalTwo = InputTwo.GetMixAndVolume.Total;
			float Max = 0;
			Vector2 TOR1 = Vector2.zero;
			Vector2 TOR2 = Vector2.zero;

			if ((ToTakeFromInputTwo / ToTakeFromInputOne) * Totalone.y > TotalTwo.y)
			{
				Max = TotalTwo.y;
				TOR1.y = (ToTakeFromInputOne / ToTakeFromInputTwo) * Max;
				TOR2.y = Max;
			}
			else
			{
				Max = Totalone.y;
				TOR1.y = Max;
				TOR2.y = (ToTakeFromInputTwo / ToTakeFromInputOne) * Max;
			}

			if (TOR1.y + TOR2.y > TransferMoles)
			{
				float Multiplier = TransferMoles / (TOR1.y + TOR2.y);
				TOR1.y *= Multiplier;
				TOR2.y *= Multiplier;
			}

			if ((ToTakeFromInputTwo / ToTakeFromInputOne) * Totalone.y > TotalTwo.y)
			{
				Max = TotalTwo.y;
				TOR1.y = (ToTakeFromInputOne / ToTakeFromInputTwo) * Max;
				TOR2.y = Max;
			}
			else
			{
				Max = Totalone.y;
				TOR1.y = Max;
				TOR2.y = (ToTakeFromInputTwo / ToTakeFromInputOne) * Max;
			}

			if (TOR1.y + TOR2.y > TransferMoles)
			{
				float Multiplier = TransferMoles / (TOR1.y + TOR2.y);
				TOR1.y *= Multiplier;
				TOR2.y *= Multiplier;
			}


			//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

			if ((ToTakeFromInputTwo / ToTakeFromInputOne) * Totalone.x > TotalTwo.x)
			{
				Max = TotalTwo.x;
				TOR1.x = (ToTakeFromInputOne / ToTakeFromInputTwo) * Max;
				TOR2.x  = Max;
			}
			else
			{
				Max = Totalone.x;
				TOR1.x  = Max;
				TOR2.x  = (ToTakeFromInputTwo / ToTakeFromInputOne) * Max;
			}

			if (TOR1.x + TOR2.x > TransferMoles)
			{
				float Multiplier = TransferMoles / (TOR1.x + TOR2.x);
				TOR1.x *= Multiplier;
				TOR2.x *= Multiplier;
			}

			InputOne.GetMixAndVolume.TransferTo(pipeData.mixAndVolume, TOR1);
			InputTwo.GetMixAndVolume.TransferTo(pipeData.mixAndVolume, TOR2);
			//0.4 0.5
			//50  0.5
		}

	}
}