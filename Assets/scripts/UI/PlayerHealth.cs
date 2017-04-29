using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHealth : MonoBehaviour
    {
        // this just temporary to get something working
        public void SetBodyTypeOverlay(BodyPart bodyPart)
        {
            foreach (DamageMonitorListener listener in UI.UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>())
            {
                if (listener.bodyPartType != bodyPart.Type)
                    continue;

                // We have received a bodypart damage update for this type of body type
                // Determine the correct overlay to apply from the damage state

                // minor
                if (bodyPart.BruteDamage > 0 && bodyPart.BruteDamage < 20)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.YellowDamageMonitorIcon;
                }
                // serious
                else if (bodyPart.BruteDamage >= 20 && bodyPart.BruteDamage < 40)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.OrangeDamageMonitorIcon;
                }
                // major
                else if (bodyPart.BruteDamage >= 40)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.RedDamageMonitorIcon;
                }
                else
                {
                    listener.GetComponent<Image>().sprite = bodyPart.GreenDamageMonitorIcon;
                }
            }
        }

        // this just temporary to get something working
        public void SetBodyPartBruteOverlay(BodyPart bodyPart)
        {
            foreach (DamageMonitorListener listener in UI.UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>())
            {
                if (listener.bodyPartType != bodyPart.Type)
                    continue;

                // We have received a bodypart damage update for this type of body type
                // Determine the correct overlay to apply from the damage state

                // minor
                if (bodyPart.BruteDamage > 0 && bodyPart.BruteDamage < 20)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.YellowDamageMonitorIcon;
                }
                // serious
                else if (bodyPart.BruteDamage >= 20 && bodyPart.BruteDamage < 40)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.OrangeDamageMonitorIcon;
                }
                // major
                else if (bodyPart.BruteDamage >= 40)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.RedDamageMonitorIcon;
                }
                else
                {
                    listener.GetComponent<Image>().sprite = bodyPart.GreenDamageMonitorIcon;
                }
            }
        }

        // this just temporary to get something working
        public void SetBodyPartBurnOverlay(BodyPart bodyPart)
        {
            foreach (DamageMonitorListener listener in UI.UIManager.Instance.GetComponentsInChildren<DamageMonitorListener>())
            {
                if (listener.bodyPartType != bodyPart.Type)
                    continue;

                // We have received a bodypart damage update for this type of body type
                // Determine the correct overlay to apply from the damage state

                // minor
                if (bodyPart.BurnDamage > 0 && bodyPart.BurnDamage < 20)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.YellowDamageMonitorIcon;
                }
                // serious
                else if (bodyPart.BurnDamage >= 20 && bodyPart.BurnDamage < 40)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.OrangeDamageMonitorIcon;
                }
                // major
                else if (bodyPart.BurnDamage >= 40)
                {
                    listener.GetComponent<Image>().sprite = bodyPart.RedDamageMonitorIcon;
                }
                else
                {
                    listener.GetComponent<Image>().sprite = bodyPart.GreenDamageMonitorIcon;
                }
            }
        }

        internal void DisplayCritScreen(int severity)
        {
            foreach (CritListener listener in UI.UIManager.Instance.GetComponentsInChildren<CritListener>())
            {
                listener.ShowCrit();
            }
        }

        internal void SetShownHealthAmountIcon(int v)
        {
            return;
        }

        internal void DisplayOxyScreen(int severity)
        {
            return;
        }

        internal void HideOxyScreen()
        {
            return;
        }

        internal void DisplayBruteScreen(int severity)
        {
            return;
        }

        internal void HideBruteScreen()
        {
            return;
        }

        internal void DisplayDeadScreen()
        {
            foreach (DeadListener listener in UI.UIManager.Instance.GetComponentsInChildren<DeadListener>())
            {
                listener.ShowDead();
            }
        }
    }
}