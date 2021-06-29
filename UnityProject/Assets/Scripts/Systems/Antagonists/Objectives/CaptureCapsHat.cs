using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Antagonists
{
    [CreateAssetMenu(menuName="ScriptableObjects/AntagObjectives/CaptureCapsHat")]
    public class CaptureCapsHat : Objective
    {
        [SerializeField]
        private List<GameObject> allowedHats = null;

        protected override void Setup()
        {

        }

        private ConnectedPlayer FindCaptain()
        {
	        var index = PlayerList.Instance.InGamePlayers.FindIndex(x => x.Job == JobType.CAPTAIN);
	        if (index != -1)
	        {
		        return PlayerList.Instance.InGamePlayers[index];
	        }

	        return null;
        }

        protected override bool CheckCompletion()
        {
	        var captain = FindCaptain();
            // No captain? Objective completed
            if (captain == null) return true;
            var inventory = captain.GameObject.GetComponent<ItemStorage>();
            //something fucked up, give them green
            if (inventory == null) return true;
            var headSlot = inventory.GetNamedItemSlot(NamedSlot.head).Item;
            // Objective completed if captain has no hat or is wearing a non-allowed hat
            if (headSlot == null || !allowedHats.Contains(headSlot.gameObject)) return true;

            return false;
        }
    }
}