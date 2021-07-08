using System.Collections;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;

namespace Spells
{
	public class MimeBox : Spell
	{
		[Tooltip("The prefab for the mime's special box.")]
		[SerializeField]
		private GameObject boxMime = default;

		protected override string FormatInvocationMessage(ConnectedPlayer caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.Name, caster.CharacterSettings.TheirPronoun(caster.Script));
		}

		public override bool ValidateCast(ConnectedPlayer caster)
		{
			if (base.ValidateCast(caster) == false) return false;

			if (caster.Script.mind.IsMiming == false)
			{
				Chat.AddExamineMsg(caster.GameObject, "You must dedicate yourself to silence first!");
				return false;
			}

			return true;
		}

		public override bool CastSpellServer(ConnectedPlayer caster)
		{
			if (base.CastSpellServer(caster) == false) return false;

			// Using our own spawn logic for mime box and handling despawn ourselves as well
			var box = Spawn.ServerPrefab(boxMime).GameObject;
			if (SpellData.ShouldDespawn)
			{
				// but also destroy when lifespan ends
				caster.Script.StartCoroutine(DespawnAfterDelay(), ref handle);

				IEnumerator DespawnAfterDelay()
				{
					yield return WaitFor.Seconds(SpellData.SummonLifespan);
					var storage = box.GetComponent<ItemStorage>();
					if (storage)
					{
						storage.ServerDropAll();
					}
					_ = Despawn.ServerSingle(box);
				}
			}
			// putting box in hand
			Inventory.ServerAdd(box, caster.Script.DynamicItemStorage.GetActiveHandSlot(), ReplacementStrategy.DropOther);

			return true;
		}
	}
}
