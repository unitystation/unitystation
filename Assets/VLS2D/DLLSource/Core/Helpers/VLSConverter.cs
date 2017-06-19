using UnityEngine;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    public static class VLSConverter
    {
        private static int PNT_BUFFER_SIZE = 128;

        public static void FromCollider3D(VLSObstructor _obstructor, Collider _collider)
        {
            _obstructor.ClearLocalVertices();

            if(_collider is BoxCollider)
            {
                From3DBoxCollider(_obstructor, _collider as BoxCollider);
                return;
            }

            if(_collider is SphereCollider)
            {
                FromSphereCollider(_obstructor, _collider as SphereCollider, _obstructor.circleResolution);
                return;
            }

            if(_collider is MeshCollider)
            {
                Debug.LogWarning("MeshCollider is not currently supported in VLS2D 4.0.");
                return;
            }

            if(_collider is CapsuleCollider)
            {
                Debug.LogWarning("CapsuleCollider is not currently supported in VLS2D 4.0.");
                return;
            }
        }

        public static void FromCollider2D(VLSObstructor _obstructor, Collider2D _collider)
        {
            _obstructor.ClearLocalVertices();

            if (_collider is BoxCollider2D)
            {
                FromBoxCollider(_obstructor, _collider as BoxCollider2D);
                return;
            }

            if (_collider is CircleCollider2D)
            {
                FromCircleCollider(_obstructor, _collider as CircleCollider2D, _obstructor.circleResolution);
                return;
            }

            if (_collider is PolygonCollider2D)
            {
                FromPolygonCollider(_obstructor, _collider as PolygonCollider2D, _obstructor.polyColliderPathIndex);
                return;
            }

            if (_collider is EdgeCollider2D)
            {
                Debug.LogWarning("EdgeCollider2D is not currently supported in VLS2D 4.0.");
                return;
            }
        }

        private static Vector2[] pntBuffer = new Vector2[PNT_BUFFER_SIZE];
        private static int pntCount = 0;

        #region Box/Box2D Colliders
        public static void From3DBoxCollider(VLSObstructor _obstructor, BoxCollider _collider)
        {
            pntBuffer[3] = (_collider.size * 0.5f);
            pntBuffer[2] = new Vector2(pntBuffer[3].x, -pntBuffer[3].y);
            pntBuffer[1] = new Vector2(-pntBuffer[3].x, -pntBuffer[3].y);
            pntBuffer[0] = new Vector2(-pntBuffer[3].x, pntBuffer[3].y);

            for (int i = 0; i < 4; i++)
                _obstructor.LocalVertex(10000, pntBuffer[i] + (Vector2)_collider.center);
        }

        public static void FromBoxCollider(VLSObstructor _obstructor, BoxCollider2D _collider)
        {
            pntBuffer[3] = (_collider.size * 0.5f);
            pntBuffer[2] = new Vector2(pntBuffer[3].x, -pntBuffer[3].y);
            pntBuffer[1] = new Vector2(-pntBuffer[3].x, -pntBuffer[3].y);
            pntBuffer[0] = new Vector2(-pntBuffer[3].x, pntBuffer[3].y);

            for (int i = 0; i < 4; i++)
                _obstructor.LocalVertex(10000, pntBuffer[i] + _collider.offset);

        }
        #endregion

        #region Sphere/Circle2D Colliders
        public static void FromCircleCollider(VLSObstructor _obstructor, CircleCollider2D _collider, int _circleResolution = 8)
        {
            pntCount = Mathf.RoundToInt((float)_circleResolution * 2f * _collider.radius);

            if (pntCount < _circleResolution)
                pntCount = _circleResolution;

            if (pntCount > PNT_BUFFER_SIZE)
                pntCount = PNT_BUFFER_SIZE;

            float angle = 0;
            float delta = 360f / (float)pntCount;

            for (int i = 0; i < pntCount; i++)
            {
                pntBuffer[i] = _collider.offset + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * _collider.radius;
                angle += delta;
            }

            for (int i = 0; i < pntCount; i++)
            {
                _obstructor.LocalVertex(10000, pntBuffer[i]);
            }
        }

        public static void FromSphereCollider(VLSObstructor _obstructor, SphereCollider _collider, int _circleResolution = 8)
        {
            pntCount = Mathf.RoundToInt((float)_circleResolution * 2f * _collider.radius);

            if (pntCount < _circleResolution)
                pntCount = _circleResolution;

            if (pntCount > PNT_BUFFER_SIZE)
                pntCount = PNT_BUFFER_SIZE;

            float angle = 0;
            float delta = 360f / (float)pntCount;

            for (int i = 0; i < pntCount; i++)
            {
                pntBuffer[i] = (Vector2)_collider.center + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * _collider.radius;
                angle += delta;
            }

            for (int i = 0; i < pntCount; i++)
            {
                _obstructor.LocalVertex(10000, pntBuffer[i]);
            }
        }
        #endregion

        public static void FromPolygonCollider(VLSObstructor _obstructor, PolygonCollider2D _collider, int _pathIndex = 0)
        {
            pntCount = _collider.GetPath(_pathIndex).Length;

            for (int i = pntCount - 1; i >= 0; i--)
                _obstructor.LocalVertex(10000, _collider.GetPath(_pathIndex)[i]);
            
            if (!_obstructor.VertsAreCounterClockwise())
                _obstructor.ReverseNormals();
        }
    }
}