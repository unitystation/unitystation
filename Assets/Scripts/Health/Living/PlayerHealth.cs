using System;
using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using Sprites;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace PlayGroup
{
    public class PlayerHealth : HealthBehaviour
    {
        //Health reporting is being performed on PlayerHealthReporting component. You should use the reporting component
        //to request health data of a particular player from the server. The reporting component also performs UI updates
        //for local players

        //Fill this in editor:
        //1 HumanHead, 1 HumanTorso & 4 HumanLimbs for a standard human
        //public Dictionary<BodyPartType, BodyPartBehaviour> BodyParts = new Dictionary<BodyPartType, BodyPartBehaviour>();
        public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

        //For now a simplified blood system will be here. To be refactored into a separate thing in the future.
        public int BloodLevel = (int)BloodVolume.NORMAL;
        private int bleedVolume;

        public bool IsBleeding { get; private set; }

        private float bleedRate = 2f;

        public PlayerNetworkActions playerNetworkActions;

        public override int ReceiveAndCalculateDamage(string damagedBy, int damage, DamageType damageType, BodyPartType bodyPartAim)
        {
            base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);

            var bodyPart = findBodyPart(bodyPartAim);//randomise a bit here?
            bodyPart.ReceiveDamage(damageType, damage);

            if (isServer)
            {
                var bloodLoss = (int)(damage * BleedFactor(damageType));
                LoseBlood(bloodLoss);

                // don't start bleeding if limb is in ok condition after it received damage
                switch (bodyPart.Severity)
                {
                    case DamageSeverity.Moderate:
                    case DamageSeverity.Bad:
                    case DamageSeverity.Critical:
                        AddBloodLoss(bloodLoss);
                        break;
                }
                if (headCritical(bodyPart))
                {
                    Crit();
                }
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
                if (BodyParts[i].Type == bodyPartAim)
                    return BodyParts[i];
            }
            //dm code quotes:
            //"no bodypart, we deal damage with a more general method."
            //"missing limb? we select the first bodypart (you can never have zero, because of chest)"
            return BodyParts.PickRandom();
        }

        private void AddBloodLoss(int amount)
        {
            if (amount <= 0)
                return;
            bleedVolume += amount;
            TryBleed();
        }

        private void TryBleed()
        {
            //don't start another coroutine when already bleeding
            if (!IsBleeding)
            {
                IsBleeding = true;
                StartCoroutine(StartBleeding());
            }
        }

        private IEnumerator StartBleeding()
        {
            while (IsBleeding)
            {
                LoseBlood(bleedVolume);

                yield return new WaitForSeconds(bleedRate);
            }
        }

        //ReduceBloodLoss for bandages and stuff in the future?
        private void StopBleeding()
        {
            bleedVolume = 0;
            IsBleeding = false;
        }

        private void LoseBlood(int amount)
        {
            if (amount <= 0)
                return;
            //            Debug.LogFormat("Lost blood: {0}->{1}", BloodLevel, BloodLevel - amount);
            BloodLevel -= amount;
            BloodSplatSize scaleOfTragedy;
            if (amount > 0 && amount < 15)
            {
                scaleOfTragedy = BloodSplatSize.small;
            }
            else if (amount >= 15 && amount < 40)
            {
                scaleOfTragedy = BloodSplatSize.medium;
            }
            else
            {
                scaleOfTragedy = BloodSplatSize.large;
            }
            if (isServer)
                EffectsFactory.Instance.BloodSplat(transform.position, scaleOfTragedy);


            if (BloodLevel <= (int)BloodVolume.SURVIVE)
                Crit();

            if (BloodLevel <= 0)
                Death();
        }

        public override void Death()
        {
            StopBleeding();
            base.Death();
        }

        public void RestoreBodyParts()
        {
            foreach (var bodyPart in BodyParts)
            {
                bodyPart.RestoreDamage();
            }
        }

        public void RestoreBlood()
        {
            BloodLevel = (int)BloodVolume.NORMAL;
        }

        public static float BleedFactor(DamageType damageType)
        {
            float random = Random.Range(-0.2f, 0.2f);
            switch (damageType)
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

        protected override void OnDeathActions()
        {
            if (CustomNetworkManager.Instance._isServer)
            {
                playerNetworkActions.RpcSpawnGhost();

                PlayerMove pM = GetComponent<PlayerMove>();
                pM.isGhost = true;
                pM.allowInput = true;
                if (LastDamagedBy == gameObject.name)
                {
                    playerNetworkActions.CmdSendAlertMessage("<color=red><b>" + gameObject.name + " commited suicide</b></color>",
                        true); //killfeed
                }
                else if (LastDamagedBy.EndsWith(gameObject.name))
                { // chain reactions
                    playerNetworkActions.CmdSendAlertMessage("<color=red><b>" + gameObject.name + " screwed himself up with some help (" +
                    LastDamagedBy
                    + ")</b></color>",
                        true); //killfeed
                }
                else
                {
                    PlayerList.Instance.UpdateKillScore(LastDamagedBy);
                    playerNetworkActions.CmdSendAlertMessage(
                        "<color=red><b>" + LastDamagedBy + "</b> has killed <b>" + gameObject.name + "</b></color>", true); //killfeed
                }
                playerNetworkActions.ValidateDropItem("leftHand", true);
                playerNetworkActions.ValidateDropItem("rightHand", true);
                if (isServer)
                    EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.large);

                //FIXME Remove for next demo
                playerNetworkActions.RespawnPlayer(10);
            }
        }
    }
}