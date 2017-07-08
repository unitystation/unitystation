using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Light2D.Examples
{
    public class RocketEngine : MonoBehaviour
    {
        public float ForcePercent;
        public float MaxForce;
        public ParticleSystem Particles;
        public Rigidbody2D Rigidbody;
        public Vector2 LocalForceDirection = Vector3.up;
        public bool IsEnabled = true;
        private ParticleSystem[] _allParticles;
        private Transform myTransform;
        private Transform rigidbodyTransform;

        private void Awake()
        {
            myTransform = transform;
            rigidbodyTransform = Rigidbody.transform;
            _allParticles = Particles.GetComponentsInChildren<ParticleSystem>();
            LocalForceDirection = LocalForceDirection.normalized;
        }

        private void Update()
        {
            foreach (var particle in _allParticles)
                particle.enableEmission = IsEnabled && ForcePercent >= Random.value;
        }

        private void FixedUpdate()
        {
            if (!IsEnabled) return;
            var pos = transform.position;
            var force = myTransform.TransformDirection(LocalForceDirection)*MaxForce*Mathf.Clamp01(ForcePercent);
            Rigidbody.AddForceAtPosition(force, pos);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawRay(transform.position, transform.TransformDirection(-LocalForceDirection));
        }
    }
}