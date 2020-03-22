using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
    [CreateAssetMenu(menuName="ScriptableObjects/Objectives/StealCapHat")]
    public class SpaceCapsHat : Objective
    {
        [SerializeField]
        private List<GameObject> allowedHats;

        protected override void Setup()
        {

        }

        protected override bool CheckCompletion()
        {
            foreach (Transform t in GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo.Objects.transform)
            {
                var player = t.GetComponent<PlayerScript>();
                if (player != null)
                {
                    var inventory = player.GetComponent<ItemStorage>();
                    var headSlot = inventory.GetNamedItemSlot(NamedSlot.head).Item;
                    var playerDetails = PlayerList.Instance.Get(player.gameObject);
                    if (playerDetails.Job == JobType.CAPTAIN &&
                        (headSlot == null || !allowedHats.Contains(headSlot.gameObject)))
                    {
                      return true;
                    } 
                }
            }

            return false;
        }
    }
}