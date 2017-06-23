using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Light2D.Examples
{
    public class LandingLeg : MonoBehaviour
    {
        public bool Release = false;
        public float ReleasedAngle;
        public float HiddenAngle;
        public HingeAutoRotator AutoRotator;

        public void Start()
        {
            if (AutoRotator == null)
                AutoRotator = GetComponent<HingeAutoRotator>();
        }

        private void Update()
        {
            AutoRotator.TargetAngle = Release ? ReleasedAngle : HiddenAngle;
        }
    }
}
