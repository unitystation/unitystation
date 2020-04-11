using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Antagonists
{
    [CreateAssetMenu(menuName="ScriptableObjects/Objectives/CaptureCapsHat")]
    public class CaptureCapsHat : Objective
    {
        [SerializeField]
        private List<GameObject> allowedHats = null;

        protected override void Setup()
        {

        }

        private ConnectedPlayer FindCaptain()
        {
            List<ConnectedPlayer> allPlayers = PlayerList.Instance.InGamePlayers;
            return PlayerList.Instance.InGamePlayers.FirstOrDefault
            (
                    player => PlayerList.Instance.Get(player.GameObject).Job == JobType.CAPTAIN
            );
        }

        protected override bool CheckCompletion()
        {

            var captain = FindCaptain();
            // No captain? Objective completed
            if (captain == null) return true;
            var inventory = captain.GameObject.GetComponent<ItemStorage>();
            var headSlot = inventory.GetNamedItemSlot(NamedSlot.head).Item;
            // Objective completed if captain has no hat or is wearing a non-allowed hat
            if (headSlot == null || !allowedHats.Contains(headSlot.gameObject)) return true;

            return false;
        }
    }
}