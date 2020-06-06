using System.Collections;

namespace Spells
{
	public class MimeBox : Spell
	{
		protected override string FormatInvocationMessage(ConnectedPlayer caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.Name, caster.CharacterSettings.TheirPronoun());
		}

		public override bool ValidateCast(ConnectedPlayer caster)
		{
			if (!base.ValidateCast(caster))
			{
				return false;
			}

			if (!caster.Script.mind.IsMiming)
			{
				Chat.AddExamineMsg(caster.GameObject, "You must dedicate yourself to silence first!");
				return false;
			}

			return true;
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			if (!base.CastSpellServer(caster))
			{
				return false;
			}
			//Using our own spawn logic for mime box and handling despawn ourselves as well
			var box = Spawn.ServerPrefab("BoxMime").GameObject;
			if (SpellData.ShouldDespawn)
			{
				//but also destroy when lifespan ends
				caster.Script.StartCoroutine(DespawnAfterDelay(), ref handle);

				IEnumerator DespawnAfterDelay()
				{
					yield return WaitFor.Seconds(SpellData.SummonLifespan);
					var storage = box.GetComponent<ItemStorage>();
					if (storage)
					{
						storage.ServerDropAll();
					}
					Despawn.ServerSingle(box);
				}
			}
			//putting box in hand
			Inventory.ServerAdd(box, caster.Script.ItemStorage.GetActiveHandSlot(), ReplacementStrategy.DropOther);

			return true;
		}
	}
}
