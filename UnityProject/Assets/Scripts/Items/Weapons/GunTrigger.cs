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
        private System.Random rnd = new System.Random();

        public void Awake()
        {
            gun = GetComponent<Gun>();
        }

        public void TriggerPull(GameObject shotBy, Vector2 target,
			BodyPartType damageZone, bool isSuicideShot)
        {
            JobType job = PlayerList.Instance.Get(shotBy).Job;
            if (PlayerList.Instance.Get(shotBy).Job == setRestriction || (setRestriction == JobType.NULL && 
            (job != JobType.CLOWN && !allowClumsy || job == JobType.CLOWN && allowClumsy)))
            {
    			gun.ServerShoot(shotBy , target, damageZone, isSuicideShot);
                    return;
            }
            else if (setRestriction == JobType.NULL && (job == JobType.CLOWN && !allowClumsy || job != JobType.CLOWN && allowClumsy))
            {
                int chance = rnd.Next(0 ,2);
                if (chance == 0)
                {
    		    	gun.ServerShoot(shotBy , target, damageZone, true);
                    return;

                }
                else
                {
            	    gun.ServerShoot(shotBy , target, damageZone, isSuicideShot);
                    return;
                }
            }
            Chat.AddExamineMsgToClient($"The {gameObject.ExpensiveName()} displays \'User authentication failed\'");
       }
    }
}