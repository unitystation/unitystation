using AddressableReferences;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Systems.Faith.Miracles
{
	public class BestowThePowerOfGod : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Bestow the power of god";
		[SerializeField] private string faithMiracleDesc = "Give all faith leaders a golden weapon that holds the power of god.";
		[SerializeField] private SpriteDataSO miracleIcon;

		[SerializeField] private GameObject goldenRevolver;
		[SerializeField] private AddressableAudioSource summonSound;

		string IFaithMiracle.FaithMiracleName
		{
			get => faithMiracleName;
			set => faithMiracleName = value;
		}

		string IFaithMiracle.FaithMiracleDesc
		{
			get => faithMiracleDesc;
			set => faithMiracleDesc = value;
		}

		SpriteDataSO IFaithMiracle.MiracleIcon
		{
			get => miracleIcon;
			set => miracleIcon = value;
		}

		public int MiracleCost { get; set; } = 690;
		public void DoMiracle()
		{
			string msg = new RichText().Color(RichTextColor.Yellow).Italic().Add("You feel... Power..");
			foreach (var dong in PlayerList.Instance.GetAlivePlayers())
			{
				var weapon = Spawn.ServerPrefab(goldenRevolver, dong.GameObject.AssumedWorldPosServer());
				if (summonSound is not null)
				{
					SoundManager.PlayNetworkedAtPos(summonSound, dong.GameObject.AssumedWorldPosServer());
				}
				foreach (var handSlot in dong.Script.Equipment.ItemStorage.GetNamedItemSlots(NamedSlot.hands))
				{
					if (handSlot.IsOccupied) continue;
					Inventory.ServerAdd(weapon.GameObject, handSlot, ReplacementStrategy.DropOther);
					Chat.AddExamineMsg(dong.GameObject, msg);
					break;
				}
			}
		}
	}
}