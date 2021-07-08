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
            var inventory = captain.GameObject.GetComponent<DynamicItemStorage>();
            //something fucked up, give them green
            if (inventory == null) return true;

            var headSlots = inventory.GetNamedItemSlots(NamedSlot.head);
            foreach (var headSlot in headSlots)
            {
	            if(headSlot.IsEmpty) continue;

	            if(allowedHats.Contains(headSlot.Item.gameObject) == false) continue;

	            //Failed captain is wearing his hat!
	            return false;
            }

            // Objective completed if captain has no hat or is wearing a non-allowed hat
            return true;
        }
    }
}