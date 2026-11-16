using System;
using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;

namespace US13.Mobs.BrainAI.ContextExtensions
{
	[Serializable]
	public sealed class SlimeContextExtension : MobBehaviorContextExtension
	{
		[NonSerialized] public SlimeCore SlimeCore;
		[NonSerialized] public SlimeEat SlimeEat;
		[NonSerialized] public readonly Dictionary<GameObject, int> GoodPlayers = new();

		public override IMobBehaviorContextExtension CreateRuntime() => new SlimeContextExtension();

		public override void Bind(MobBehaviorContext context, LivingHealthMasterBase health)
		{
			base.Bind(context, health);
			SlimeCore = context.AI.GetBodyPartBehavior<SlimeCore>();
			SlimeEat = null;
			foreach (var part in health.BodyPartList)
			{
				if (part == null) continue;
				SlimeEat = part.GetBodyPartBehavior<SlimeEat>();
				if (SlimeEat != null) break;
			}
		}

		public override void Unbind()
		{
			SlimeCore = null;
			SlimeEat = null;
			base.Unbind();
		}

		public void CopyFamiliarityFrom(SlimeContextExtension source)
		{
			if (ReferenceEquals(source, this)) return;
			GoodPlayers.Clear();
			foreach (var player in source.GoodPlayers) GoodPlayers[player.Key] = player.Value;
		}
	}
}
