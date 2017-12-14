using UnityEngine;

namespace Light2D.Examples
{
    public class HingeAutoRotator : MonoBehaviour
    {
        private Rigidbody2D _jointRigidbody;
        public HingeJoint2D Joint;
        public float MaxSpeed = 360;
        public float Speed = 1;
        public float TargetAngle;
        public bool WorldAngle = true;

        private void Awake()
        {
            if (Joint == null)
            {
                Joint = GetComponent<HingeJoint2D>();
            }
            _jointRigidbody = Joint.GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            var targetAngle = WorldAngle ? TargetAngle : _jointRigidbody.rotation - TargetAngle;
            var rotTarget = Mathf.DeltaAngle(targetAngle, Joint.connectedBody.rotation);
            var motor = Joint.motor;
            motor.motorSpeed = Mathf.Clamp(-rotTarget * Speed, -MaxSpeed, MaxSpeed);
            Joint.motor = motor;
        }
    }
}