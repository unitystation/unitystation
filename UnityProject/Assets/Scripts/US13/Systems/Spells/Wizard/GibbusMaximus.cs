using US13.Core.Chat;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.UI.Core.ProgressBar;
using Util;

namespace US13.Systems.Spells.Wizard
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
						creature.OnGib(   " a Wizard.. well GibbusMaximus Spell " );
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