using HealthV2;
using Random = UnityEngine.Random;

namespace Systems.Spells.Wizard
{
	public class GibbusMaximus : Spell
	{
		public override bool CastSpellServer(PlayerInfo caster)
		{
			var creatures = MatrixManager.GetAdjacent<LivingHealthMasterBase>(caster.GameObject.AssumedWorldPosServer().CutToInt(), true);
			if (creatures.Count == 0)
			{
				Chat.AddExamineMsg(caster.GameObject, "There are no creatures nearby to harvest meat from!");
				return false;
			}
			Chat.AddChatMsgToChatServer(caster, "Giii uss, riss toss..", ChatChannel.Local);
			var progress = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Escape), () =>
			{
				var creatures = MatrixManager.GetAdjacent<LivingHealthMasterBase>(caster.GameObject.AssumedWorldPosServer().CutToInt(), true);
				foreach (var creature in creatures)
				{
					if (creature.IsDead)
					{
						creature.OnGib();
					}
				}
				Chat.AddChatMsgToChatServer(caster, "..GIBBUSS, MAXIMUS!!", ChatChannel.Local, Loudness.MEGAPHONE);
			});
			progress.ServerStartProgress(gameObject.AssumedWorldPosServer(), 24f,
				caster.GameObject);
			return true;
		}
	}
}