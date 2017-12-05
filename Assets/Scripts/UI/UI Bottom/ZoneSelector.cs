using PlayGroup;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{

    //    public enum DamageZoneSelector {
    //        torso,
    //        head,
    //        eyes,
    //        mouth,
    //        r_arm,
    //        l_arm,
    //        r_leg,
    //        l_leg
    //    }

    public class ZoneSelector : MonoBehaviour
    {
        public Sprite[] selectorSprites;
        public Image selImg;

        private void Start()
        {
            //init chest selection
            SelectAction(1, false);
        }

        //unity...
        public void SelectAction(int curSelect)
        {
            SelectAction(curSelect, true);
        }

        public void SelectAction(int curSelect, bool click)
        {
            if (click) { SoundManager.Play("Click01"); }
            selImg.sprite = selectorSprites[curSelect];
            UIManager.DamageZone = (BodyPartType)curSelect;
        }
    }
}