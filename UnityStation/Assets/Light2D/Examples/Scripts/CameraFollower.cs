using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace Light2D.Examples
{
    public class CameraFollower : MonoBehaviour
    {
        public Rigidbody2D Followed;
        public float CameraPositionLerp = 0.02f;
        public float VelocityMul = 1;
        public float VelocitySmoothnessLerp = 0.9f;
        public float MinAccountedSpeed = 10;
        public float CamBordersMul = 0.8f;
        public float InstantJumpDistance = 50;
        private Transform _cameraTransform;
        private Vector2 _smoothVelocity;
        private Camera _camera;

        private void OnEnable()
        {
            _camera = Camera.main;
            _cameraTransform = _camera.transform;
            _cameraTransform.position = _cameraTransform.position.WithXY(Followed.position);
        }

        private void Start()
        {
            _cameraTransform.position = _cameraTransform.position.WithXY(Followed.position);
        }

        private void Update()
        {
            if (Followed != null)
            {
                var camPos = _cameraTransform.position;
                var followedPos = Followed.position;

                var vel = Followed.velocity.sqrMagnitude > MinAccountedSpeed*MinAccountedSpeed
                    ? Followed.velocity
                    : Vector2.zero;
                _smoothVelocity = Vector2.Lerp(vel, _smoothVelocity, VelocitySmoothnessLerp);

                var camTargetPos = followedPos + _smoothVelocity*VelocityMul;
                var camHalfWidth = _camera.orthographicSize*_camera.aspect*CamBordersMul;
                var camHalfHeight = _camera.orthographicSize*CamBordersMul;
                var followedDir = followedPos - camTargetPos;

                if (followedDir.x > camHalfWidth)
                    camTargetPos.x = followedPos.x - camHalfWidth;
                if (followedDir.x < -camHalfWidth)
                    camTargetPos.x = followedPos.x + camHalfWidth;
                if (followedDir.y > camHalfHeight)
                    camTargetPos.y = followedPos.y - camHalfHeight;
                if (followedDir.y < -camHalfHeight)
                    camTargetPos.y = followedPos.y + camHalfHeight;

                var pos = (followedPos - (Vector2) camPos).sqrMagnitude < InstantJumpDistance*InstantJumpDistance
                    ? Vector2.Lerp(camPos, camTargetPos, CameraPositionLerp*Time.deltaTime)
                    : followedPos;

                _cameraTransform.position = camPos.WithXY(pos);
            }
        }
    }
}