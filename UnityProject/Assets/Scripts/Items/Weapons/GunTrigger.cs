using System;
using UnityEngine;


namespace Weapons
{
    public class GunTrigger : MonoBehaviour
    {
        [SerializeField]
        private JobType setRestriction;
        private Gun gun;
        [SerializeField]
        private bool allowClumsy;
        [SerializeField]
        private bool allowNonClumsy;
        private System.Random rnd = new System.Random();

        public void Awake()
        {
            gun = GetComponent<Gun>();
        }

        public void TriggerPull(GameObject shotBy, Vector2 target,
			BodyPartType damageZone, bool isSuicideShot)
        {
            JobType job = PlayerList.Instance.Get(shotBy).Job;

            if (setRestriction == JobType.NULL)
            {
	            if (job == JobType.CLOWN && !allowClumsy)
	            {
		            int chance = rnd.Next(0 ,2);
		            if (chance == 0)
		            {
			            gun.ServerShoot(shotBy , target, damageZone, true);
			            Chat.AddActionMsgToChat(
				            shotBy,
				            "You fumble up and shoot yourself!",
				            $"{shotBy.ExpensiveName()} fumbles up and shoots themself!");
			            return;
		            }
	            }
	            else if (job != JobType.CLOWN && !allowNonClumsy)
	            {
		            gun.ServerShoot(shotBy , target, damageZone, true);
		            Chat.AddActionMsgToChat(
			            shotBy,
			            "You somehow shoot yourself in the face! How the hell?!",
			            $"{shotBy.ExpensiveName()} somehow manages to shoot themself in the face!");
		            return;
	            }
            }
            gun.ServerShoot(shotBy , target, damageZone, isSuicideShot);
        }
    }
}