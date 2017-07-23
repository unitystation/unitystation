using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D.Examples
{
    public class RotationTweener : MonoBehaviour
    {
        public float AngularSpeed;

        void Update()
        {
            transform.rotation *= Quaternion.Euler(0, 0, AngularSpeed*Time.deltaTime);
        }
    }
}
