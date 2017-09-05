using System;
using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using Sprites;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Objects
{
    public class HumanHealthBehaviour : HealthBehaviour
    {
        //Fill this in editor:
        //1 HumanHead, 1 HumanTorso & 4 HumanLimbs for a standard human
        //public Dictionary<BodyPartType, BodyPartBehaviour> BodyParts = new Dictionary<BodyPartType, BodyPartBehaviour>();
        public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

        //For now a simplified blood system will be here. To be refactored into a separate thing in the future.
        public int BloodLevel = (int) BloodVolume.NORMAL;
        private int _bleedRate;
        public bool IsBleeding { get; private set; }

        [Server]
        public override int ReceiveAndCalculateDamage(string damagedBy, int damage, DamageType damageType, BodyPartType bodyPartAim)
        {
            base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);

            BodyPartBehaviour bodyPart = findBodyPart(bodyPartAim);//randomise a bit here?
            bodyPart.ReceiveDamage(damageType, damage);
            switch ( bodyPart.Severity )
            {
                    case DamageSeverity.Moderate: 
                    case DamageSeverity.Bad:
                    case DamageSeverity.Critical: 
                        AddBloodLoss(( int ) ( damage * BleedFactor(damageType) )); 
                        break;
            }
            if ( headCritical(bodyPart) )
            {
                Crit();
            }

            return damage;

        }

        private static bool headCritical(BodyPartBehaviour bodyPart)
        {
            return bodyPart.Type.Equals(BodyPartType.HEAD) && bodyPart.Severity == DamageSeverity.Critical;
        }

        private BodyPartBehaviour findBodyPart(BodyPartType bodyPartAim)
        {
            //Don't like how you should iterate through bodyparts each time, but inspector doesn't seem to like dicts
            for (int i = 0; i < BodyParts.Count; i++)
            {
                if (BodyParts[i].Type == bodyPartAim) return BodyParts[i];
            }
            //dm code quotes:
            //"no bodypart, we deal damage with a more general method."
            //"missing limb? we select the first bodypart (you can never have zero, because of chest)"
            return BodyParts.PickRandom();
        }
        
//        private BodyPartBehaviour findBodyPart(BodyPartType bodyPartAim)
//        {
//            if ( BodyParts[bodyPartAim] )
//            {
//                return BodyParts[bodyPartAim];
//            }
//            //dm code quotes:
//            //"no bodypart, we deal damage with a more general method."
//            //"missing limb? we select the first bodypart (you can never have zero, because of chest)"
//            return BodyParts.Values.PickRandom();
//        }

        /// <summary>
        /// to be run from some kind of coroutine each n seconds
        /// </summary>
        private void UpdateHealth()
        {
            LoseBlood(_bleedRate);
        }

        private void AddBloodLoss(int amount)
        {
            if(amount <= 0) return;
            LoseBlood(amount); //mwahaha
            _bleedRate += amount;
            TryBleed();
        }

        private void TryBleed()
        {
            if ( !IsBleeding )
            {
                IsBleeding = true;
                StartCoroutine(StartBleeding());
            }
        }

        private IEnumerator StartBleeding()
        {
            while ( IsBleeding )
            {
                UpdateHealth();
                yield return new WaitForSeconds(2f);
            }
        }

        //ReduceBloodLoss for bandages and stuff in the future?

        private void StopBleeding()
        {
            _bleedRate = 0;
            IsBleeding = false;
        }

        private void LoseBlood(int amount)
        {
            if(amount <= 0) return;
            Debug.LogFormat("Lost blood: {0}->{1}", BloodLevel, BloodLevel - amount);
            BloodLevel -= amount;
            BloodSplatSize scaleOfTragedy;
            if      ( amount > 0 && amount < 15 )   {scaleOfTragedy = BloodSplatSize.small;}
            else if ( amount >= 15 && amount < 40 ) {scaleOfTragedy = BloodSplatSize.medium;}
            else                                    {scaleOfTragedy = BloodSplatSize.large;}
            BloodSplat(scaleOfTragedy);


            if(BloodLevel <= (int)BloodVolume.SURVIVE)
            {
                OnCritActions();
            }

            if ( BloodLevel <= 0 )
            {
                Death();
            }
        }

        protected override void Death()
        {
            StopBleeding();
            base.Death();
        }

        public void RestoreBodyParts()
        {
//            foreach ( var bodyPart in BodyParts.Values )
            foreach ( var bodyPart in BodyParts )
            {
                bodyPart.RestoreDamage();
            }
        }

        public void RestoreBlood()
        {
            BloodLevel = ( int ) BloodVolume.NORMAL;
        }

        public static float BleedFactor(DamageType damageType)
        {
            float random = Random.Range(-0.2f,0.2f);
            switch ( damageType )
            {
                case DamageType.BRUTE:
                    return 0.6f + random;
                case DamageType.BURN:
                    return 0.4f + random;
                case DamageType.TOX:
                    return 0.2f + random;
            }
            return 0;
        }

        /// a copypaste from Human
        public override void OnDeathActions()
        {
            if (CustomNetworkManager.Instance._isServer)
            {
                PlayerNetworkActions pna = GetComponent<PlayerNetworkActions>();
                pna.RpcSpawnGhost();

                PlayerMove pM = GetComponent<PlayerMove>();
                pM.isGhost = true;
                pM.allowInput = true;
                if ( LastDamagedBy == gameObject.name )
                {
                    pna.CmdSendAlertMessage( "<color=red><b>" + gameObject.name + " commited suicide</b></color>",
                        true ); //killfeed
                }
                else if(LastDamagedBy.EndsWith( gameObject.name )) // chain reactions
                {
                    pna.CmdSendAlertMessage( "<color=red><b>" + gameObject.name + " screwed himself up with some help (" + 
                                             LastDamagedBy
                                             + ")</b></color>",
                        true ); //killfeed
                } 
                else 
                {
                    PlayerList.Instance.UpdateKillScore( LastDamagedBy );
                    pna.CmdSendAlertMessage(
                        "<color=red><b>" + LastDamagedBy + "</b> has killed <b>" + gameObject.name + "</b></color>", true ); //killfeed
                }
                
                GetComponent<PlayerNetworkActions>().CmdDropItem("leftHand");
                GetComponent<PlayerNetworkActions>().CmdDropItem("rightHand");
                BloodSplat(BloodSplatSize.large);
                pna.RespawnPlayer(10);
            }
        }

        ///a copypaste from Living
        private void BloodSplat( BloodSplatSize splatSize )
        {
            GameObject b = Instantiate(Resources.Load( "BloodSplat" ) as GameObject, transform.position, Quaternion.identity);
            NetworkServer.Spawn(b);
            BloodSplat bSplat = b.GetComponent<BloodSplat>();
            //TODO streaky blood from bullet wounds, dragging blood drops etc
            //choose a random blood sprite
            int spriteNum = 0;
            switch (splatSize) {
                case BloodSplatSize.small:
                    spriteNum = Random.Range(137, 139);
                    break;
                case BloodSplatSize.medium:
                    spriteNum = Random.Range(116, 120);
                    break;
                case BloodSplatSize.large:
                    spriteNum = Random.Range(105, 108);
                    break;
            }

            if(spriteNum != 0)
                bSplat.bloodSprite = spriteNum;
        }
    }
}