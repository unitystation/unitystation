using UnityEngine;

namespace Core.Pathfinding
{
    public static class Utils
    {
        public const float Epsilon = 0.00001f;

        public static readonly Vector3Int[] FourDirections =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0)
        };

        public static readonly Vector3Int[] EightDirections =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0)
        };

        public static readonly Vector3Int[] DiagonalDirections =
        {
            new Vector3Int(1, -1, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(-1, 1, 0),
            new Vector3Int(1, 1, 0)
        };

        public static bool IsZero(float f)
        {
            return System.Math.Abs(f) < Epsilon;
        }

        public static float Sign(float f)
        {
            return IsZero(f) ? 0 : Mathf.Sign(f);
        }

        /// <summary>
        /// Converts the vector to an angle in radians.
        /// </summary>
        public static float Vector2ToAngle(Vector2 v)
        {
            return Mathf.Atan2(v.y, v.x);
        }

        /// <summary>
        /// Converts the angle in radians to a vector.
        /// </summary>
        public static Vector2 AngleToVector2(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public static float RandomBinomial()
        {
            return Random.value - Random.value;
        }

        public static T GetComponentAtMouse3D<T>()
        {
            T target = default(T);

            Ray ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, float.PositiveInfinity))
            {
                target = hit.transform.GetComponentInParent<T>();
            }

            return target;
        }

        public static T GetComponentAtPosition3D<T>(Vector3 position)
        {
            T target = default(T);

            Collider[] colliders = Physics.OverlapSphere(position, 0.01f);

            if (colliders.Length > 0)
            {
                target = colliders[0].GetComponentInParent<T>();
            }

            return target;
        }

        public static T GetComponentAtMouse2D<T>()
        {
            Vector3 worldPoint = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return GetComponentAtPosition2D<T>(worldPoint);
        }

        public static T GetComponentAtPosition2D<T>(Vector3 position)
        {
            T target = default(T);

            Collider2D col = Physics2D.OverlapPoint(position);

            if (col != null)
            {
                target = col.GetComponentInParent<T>();
            }

            return target;
        }

        /// <summary>
        /// Casts a 3D ray with queriesHitBackfaces set to false for just
        /// the raycast without affecting the project setting.
        /// </summary>
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float distance, int layerMask)
        {
            bool origQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = false;

            bool result = Physics.Raycast(origin, direction, out hitInfo, distance, layerMask);

            Physics.queriesHitBackfaces = origQueriesHitBackfaces;

            return result;
        }

        /// <summary>
        /// Casts a 3D ray with queriesHitBackfaces set to false for just
        /// the raycast without affecting the project setting.
        /// </summary>
        public static bool Raycast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
        {
            Vector3 dir = end - start;
            return Raycast(start, dir, out hitInfo, dir.magnitude, layerMask);
        }

        /// <summary>
        /// Casts a sphere with queriesHitBackfaces set to false for just
        /// the sphere cast without affecting the project setting.
        /// </summary>
        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float distance, int layerMask)
        {
            bool origQueriesHitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = false;

            bool result = Physics.SphereCast(origin, radius, direction, out hitInfo, distance, layerMask);

            Physics.queriesHitBackfaces = origQueriesHitBackfaces;

            return result;
        }

        public static bool SphereCast(Vector3 start, Vector3 end, float radius, out RaycastHit hitInfo, int layerMask)
        {
            Vector3 dir = end - start;
            return SphereCast(start, radius, dir, out hitInfo, dir.magnitude, layerMask);
        }

        /// <summary>
        /// Casts a 2D ray with queriesStartInColliders set to false for just
        /// the raycast without affecting the project setting.
        /// </summary>
        public static RaycastHit2D Raycast2D(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
            bool origQueriesStartInColliders = Physics2D.queriesStartInColliders;
            Physics2D.queriesStartInColliders = false;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, layerMask);

            Physics2D.queriesStartInColliders = origQueriesStartInColliders;

            return hit;
        }

        /// <summary>
        /// Casts a 2D ray with queriesStartInColliders set to false for just
        /// the raycast without affecting the project setting.
        /// </summary>
        public static RaycastHit2D Raycast2D(Vector2 start, Vector2 end, int layerMask)
        {
            Vector2 dir = end - start;
            return Raycast2D(start, dir, dir.magnitude, layerMask);
        }

        /// <summary>
        /// Casts a circle with queriesStartInColliders set to false for just
        /// the circle cast without affecting the project setting.
        /// </summary>
        public static RaycastHit2D CircleCast(Vector2 origin, float radius, Vector2 direction, float distance, int layerMask)
        {
            bool origQueriesStartInColliders = Physics2D.queriesStartInColliders;
            Physics2D.queriesStartInColliders = false;

            RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, layerMask);

            Physics2D.queriesStartInColliders = origQueriesStartInColliders;

            return hit;
        }

        /// <summary>
        /// Casts a circle with queriesStartInColliders set to false for just
        /// the circle cast without affecting the project setting.
        /// </summary>
        public static RaycastHit2D CircleCast(Vector2 start, Vector2 end, float radius, int layerMask)
        {
            Vector2 dir = end - start;
            return CircleCast(start, radius, dir, dir.magnitude, layerMask);
        }

        public static void DebugDrawCross(Vector3 position, float size, Color color = default(Color), float duration = 0.0f, bool depthTest = true)
        {
            Debug.DrawRay(position - Vector3.right * size / 2, Vector3.right * size, color, duration, depthTest);
            Debug.DrawRay(position - Vector3.down * size / 2, Vector3.down * size, color, duration, depthTest);
            Debug.DrawRay(position - Vector3.forward * size / 2, Vector3.forward * size, color, duration, depthTest);
        }
    }
}