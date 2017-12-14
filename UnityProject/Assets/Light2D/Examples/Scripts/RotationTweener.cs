using UnityEngine;

namespace Light2D.Examples
{
    public class RotationTweener : MonoBehaviour
    {
        public float AngularSpeed;

        private void Update()
        {
            transform.rotation *= Quaternion.Euler(0, 0, AngularSpeed * Time.deltaTime);
        }
    }
}