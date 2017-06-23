using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Light2D.Examples
{
    public class SpiderLeg : MonoBehaviour
    {
        public Rigidbody2D Body;
        public Transform ConnectedTransform;
        public Vector2 ConnectedAnchor;
        public float MaxForce = 5000;
        public float MaxMoveSpeed = 10;
        public float TargetLength = 10;
        public float Spring = 100;
        public float Damper = 10;
        private Transform _transform;

        private void Awake()
        {
            _transform = transform;
        }

        private void LateUpdate()
        {
            if (ConnectedTransform == null || Body == null)
                return;

            Vector2 worldAnchor = ConnectedTransform.TransformPoint(ConnectedAnchor);
            Vector2 worldOrigin = Body.position;
            var length = (worldAnchor - worldOrigin).magnitude;

            _transform.position = worldOrigin;
            _transform.localScale = transform.localScale.WithY(length);
            _transform.rotation = Quaternion.Euler(0, 0, (worldOrigin - worldAnchor).AngleZ());
        }

        private void FixedUpdate()
        {
            if (ConnectedTransform == null || Body == null)
                return;

            Vector2 worldAnchor = ConnectedTransform.TransformPoint(ConnectedAnchor);
            Vector2 worldOrigin = Body.position;

            var length = (worldAnchor - worldOrigin).magnitude;
            var force = (TargetLength - length)*Spring;
            force -= Body.velocity.magnitude*Damper*Mathf.Sign(force);
            force = Mathf.Clamp(force, -MaxForce, MaxForce);
            var forceVec = (Body.position - worldAnchor)/length*force;

            Body.AddForce(forceVec, ForceMode2D.Force);
        }

        private void OnDrawGizmos()
        {
            if (ConnectedTransform == null || Body == null)
                return;

            Vector2 worldAnchor = ConnectedTransform.TransformPoint(ConnectedAnchor);
            Vector2 worldOrigin = Body.position;

            Gizmos.DrawLine(worldAnchor, worldOrigin);
        }
    }
}