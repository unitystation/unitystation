using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Light2D
{
    /// <summary>
    /// Bunch of utility functions that could be userful sometimes.
    /// </summary>
    public static class Util
    {
#if UNITY_METRO && !UNITY_EDITOR
    static Windows.Devices.Input.TouchCapabilities touchCaps = new Windows.Devices.Input.TouchCapabilities();
#endif

        public static bool isTouchscreen
        {
            get
            {
                return
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
                    false;
#elif UNITY_METRO
            touchCaps.TouchPresent != 0;
#else
            true;
#endif
            }
        }

        public static void SafeIterateBackward<T>(this IList<T> list, Action<T> action)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                action(list[i]);
            }
        }

        public static void SafeIterateBackward<T>(this IList<T> list, Action<T, int> action)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                action(list[i], i);
            }
        }

        public static void SafeIterateBackward<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            var list = enumerable.ToArray();
            for (int i = list.Length - 1; i >= 0; i--)
            {
                action(list[i]);
            }
        }

        public static void SafeIterateBackward<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            var list = enumerable.ToArray();
            for (int i = list.Length - 1; i >= 0; i--)
            {
                action(list[i], i);
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var obj in enumerable)
            {
                action(obj);
                yield return obj;
            }
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            var i = 0;
            foreach (var obj in enumerable)
            {
                action(obj, i);
                yield return obj;
                i++;
            }
        }

        public static IEnumerable<T> GetComponentsInChildRecursive<T>(this GameObject root) where T : Component
        {
            return root.GetChildRecursive().SelectMany(go => go.GetComponents<T>());
        }

        public static IEnumerable<GameObject> GetChildRecursive(this GameObject root)
        {
            foreach (Transform child in root.transform)
            {
                var cgo = child.gameObject;
                yield return cgo;
                foreach (var gameObject in cgo.GetChildRecursive())
                    yield return gameObject;
            }
        }

        public static Rigidbody2D GetRigidbodyUnderCursor()
        {
            var mousePos = GetMousePosInUnits();
            var click = Physics2D.OverlapPoint(mousePos);
            return click != null ? click.attachedRigidbody : null;
        }

        public static Vector2 GetMousePosInUnits()
        {
            var mouse = Input.mousePosition;
            var camera = Camera.main;
            var mouseWorld = camera.ScreenToWorldPoint(
                new Vector3(mouse.x, mouse.y, -camera.transform.position.z));
            return mouseWorld;
        }

        public static Vector2 ScreenToWorld(Vector2 screen)
        {
            var camera = Camera.main;
            var mouseWorld = camera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -camera.transform.position.z));
            return mouseWorld;
        }

        public static Vector2 WorldToScreen(Vector2 screen)
        {
            var camera = Camera.main;
            var mouseWorld = camera.WorldToScreenPoint(
                new Vector3(screen.x, screen.y, -camera.transform.position.z));
            return mouseWorld;
        }

        public static GameObject Instantiate(GameObject prefab)
        {
            return (GameObject) Object.Instantiate(prefab);
        }

        public static T Instantiate<T>(GameObject prefab) where T : Component
        {
            return (Instantiate(prefab)).GetComponent<T>();
        }

        public static T Instantiate<T>(GameObject prefab, Vector3 position, Quaternion rotation)
            where T : Component
        {
            return ((GameObject) Object.Instantiate(prefab, position, rotation)).GetComponent<T>();
        }

        public static float ClampAngle(float angle)
        {
            angle = Mathf.Repeat(angle, 360);
            if (angle > 180) angle -= 360;
            return angle;
        }

        public static float AngleZ(this Vector2 angle)
        {
            if (angle == Vector2.zero) return 0;
            return Vector2.Angle(Vector2.up, angle)*Mathf.Sign(-angle.x);
        }

        public static float AngleZ(this Vector3 angle)
        {
            if (angle == Vector3.zero) return 0;
            return Vector2.Angle(Vector2.up, angle)*Mathf.Sign(-angle.x);
        }

        public static float Proj(Vector2 vector, Vector2 onNormal)
        {
            return Vector2.Dot(vector, onNormal)*onNormal.magnitude;
        }

        public static float Cross(Vector2 lhs, Vector2 rhs)
        {
            return lhs.x*rhs.y - lhs.y*rhs.x;
        }

        public static void Destroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (obj is GameObject)
                ((GameObject) obj).transform.parent = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                GameObject.DestroyImmediate(obj);
            else GameObject.Destroy(obj);
#else
        GameObject.Destroy(obj);
#endif
        }

        public static T RandomElement<T>(this IList<T> coll)
        {
            var index = Random.Range(0, coll.Count);
            return coll[index];
        }

        public static T RandomElement<T>(this T[] coll)
        {
            var index = Random.Range(0, coll.Length);
            return coll[index];
        }

        public static Vector2 RotateZ(this Vector2 v, float angle)
        {
            float sin = Mathf.Sin(angle*Mathf.Deg2Rad);
            float cos = Mathf.Cos(angle*Mathf.Deg2Rad);
            float tx = v.x;
            float ty = v.y;
            return new Vector2((cos*tx) - (sin*ty), (cos*ty) + (sin*tx));
        }

        public static Vector3 RotateZ(this Vector3 v, float angle)
        {
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            float tx = v.x;
            float ty = v.y;
            return new Vector3((cos*tx) - (sin*ty), (cos*ty) + (sin*tx), v.z);
        }

        public static Vector2 Rotate90(this Vector2 v)
        {
            return new Vector2(-v.y, v.x);
        }

        public static void Log(params object[] vals)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < vals.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(vals[i]);
            }
            Debug.Log(sb.ToString());
        }

        public static void Log(UnityEngine.Object context, params object[] vals)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < vals.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append(vals[i]);
            }
            Debug.Log(sb.ToString(), context);
        }

        public static void LogArray<T>(IEnumerable<T> enumerable)
        {
            var sb = new StringBuilder();
            var vals = enumerable.ToArray();
            for (int i = 0; i < vals.Length; i++)
            {
                sb.Append(i);
                sb.Append(": ");
                sb.Append(vals[i]);
                sb.AppendLine(";");
            }
            Debug.Log(sb.ToString());
        }

        public static Color Set(this Color color, int channel, float value)
        {
            color[channel] = value;
            return color;
        }

        public static Color WithAlpha(this Color color, float value)
        {
            color.a = value;
            return color;
        }

        public static Vector3 WithX(this Vector3 vec, float value)
        {
            vec.x = value;
            return vec;
        }

        public static Vector3 WithXY(this Vector3 vec, Vector2 xy)
        {
            vec.x = xy.x;
            vec.y = xy.y;
            return vec;
        }

        public static Vector3 WithXY(this Vector3 vec, float x, float y)
        {
            vec.x = x;
            vec.y = y;
            return vec;
        }

        public static Vector3 WithY(this Vector3 vec, float value)
        {
            vec.y = value;
            return vec;
        }

        public static Vector3 WithZ(this Vector3 vec, float value)
        {
            vec.z = value;
            return vec;
        }

#if !UNITY_WINRT
        public static void Serialize<T>(string path, T obj) where T : class
        {
            using (var stream = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof (T));
                var xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
                serializer.Serialize(xmlWriter, obj);
            }
        }

        public static byte[] Serialize<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof (T));
                var xmlWriter = new XmlTextWriter(stream, Encoding.UTF8);
                serializer.Serialize(xmlWriter, obj);
                return stream.ToArray();
            }
        }

        public static T Deserialize<T>(string path) where T : class
        {
            using (var stream = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof (T));
                var fromFile = serializer.Deserialize(stream) as T;
                return fromFile;
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            try
            {
                using (var stream = new MemoryStream(data))
                {
                    var serializer = new XmlSerializer(typeof (T));
                    var fromFile = (T) serializer.Deserialize(stream);
                    return fromFile;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return default(T);
            }
        }
#endif

        public static int IndexOfMin<T>(this List<T> list, Func<T, float> pred)
        {
            int minId = -1;
            float minVal = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                var val = pred(obj);
                if (val < minVal)
                {
                    minId = i;
                    minVal = val;
                }
            }
            return minId;
        }

        public static T MinBy<T>(this IEnumerable<T> list, Func<T, float> pred)
        {
            T minObj = default(T);
            float minVal = float.MaxValue;
            bool isEmpty = true;
            foreach (var obj in list)
            {
                var val = pred(obj);
                if (val < minVal)
                {
                    minObj = obj;
                    minVal = val;
                }
                isEmpty = false;
            }
            if (isEmpty) throw new ArgumentException();
            return minObj;
        }

        public static T MinByOrDefault<T>(this IEnumerable<T> list, Func<T, float> pred)
        {
            T minObj = default(T);
            float minVal = float.MaxValue;
            bool isEmpty = true;
            foreach (var obj in list)
            {
                var val = pred(obj);
                if (val < minVal)
                {
                    minObj = obj;
                    minVal = val;
                }
                isEmpty = false;
            }
            if (isEmpty) return default (T);
            return minObj;
        }

        public static Vector2 NearestPointOnLine(this Vector2 c, Vector2 a, Vector2 b)
        {
            var v = (a - b).normalized;
            return b + v*Vector2.Dot(v, c - b);
        }

        public static float DistToLine(this Vector2 c, Vector2 a, Vector2 b)
        {
            var n = new Vector2(b.y - a.y, a.x - b.x).normalized;
            var v = c - a;
            return Vector2.Dot(n, v);
        }

        public static bool GetTouchByFingerId(int fingerId, out Touch resultTouch)
        {
            if (fingerId == -1)
            {
                resultTouch = new Touch();
                return false;
            }
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId == fingerId)
                {
                    resultTouch = touch;
                    return true;
                }
            }
            resultTouch = new Touch();
            return false;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            angle = Mathf.Repeat(angle, 360);
            min = Mathf.Repeat(min, 360);
            max = Mathf.Repeat(max, 360);

            if (min > max)
            {
                if (angle > min || angle < max) return angle;
                return angle > (min + max)/2f ? min : max;
            }

            if (angle > min && angle < max) return angle;
            return angle < min ? min : max;
        }

        public static int Hash<T>(T v1, T v2, T v3, T v4)
        {
            int hash = 23;
            hash = hash*31 + v1.GetHashCode();
            hash = hash*31 + v2.GetHashCode();
            hash = hash*31 + v3.GetHashCode();
            hash = hash*31 + v4.GetHashCode();
            return hash;
        }

        public static int Hash<T>(T v1, T v2, T v3)
        {
            int hash = 23;
            hash = hash*31 + v1.GetHashCode();
            hash = hash*31 + v2.GetHashCode();
            hash = hash*31 + v3.GetHashCode();
            return hash;
        }

        public static int Hash<T>(T v1, T v2)
        {
            int hash = 23;
            hash = hash*31 + v1.GetHashCode();
            hash = hash*31 + v2.GetHashCode();
            return hash;
        }

        public static int Hash<T>(params T[] els)
        {
            int hash = 23;
            for (int i = 0; i < els.Length; i++)
                hash = hash*31 + els[i].GetHashCode();
            return hash;
        }

        public static Vector4 Div(this Vector4 vec, Vector4 div)
        {
            return new Vector4(vec.x/div.x, vec.y/div.y, vec.z/div.z, vec.w/div.w);
        }

        public static Vector3 Div(this Vector3 vec, Vector3 div)
        {
            return new Vector3(vec.x/div.x, vec.y/div.y, vec.z/div.z);
        }

        public static Vector2 Div(this Vector2 vec, Vector2 div)
        {
            return new Vector2(vec.x/div.x, vec.y/div.y);
        }

        public static Vector4 Mul(this Vector4 v1, Vector4 v2)
        {
            return Vector4.Scale(v1, v2);
        }

        public static Vector3 Mul(this Vector3 v1, Vector3 v2)
        {
            return Vector3.Scale(v1, v2);
        }

        public static Vector2 Mul(this Vector2 v1, Vector2 v2)
        {
            return Vector2.Scale(v1, v2);
        }

        public static float DecodeFloatRGBA(Vector3 enc)
        {
            enc = new Vector3((byte) (enc.x*254f), (byte) (enc.y*254f), (byte) (enc.z*254f))/255f;
            var kDecodeDot = new Vector4(1f, 1/255f, 1/65025f);
            var result = Vector3.Dot(enc, kDecodeDot);
            return result;
        }

        public static Vector4 EncodeFloatRGBA(float v)
        {
            var kEncodeMul = new Vector3(1.0f, 255.0f, 65025.0f);
            var enc = kEncodeMul*v;
            enc = new Vector3(
                enc.x - Mathf.Floor(enc.x), enc.y - Mathf.Floor(enc.y),
                enc.z - Mathf.Floor(enc.z));
            return enc;
        }

#if UNITY_EDITOR
        public static bool IsSceneViewFocused
        {
            get
            {
                return SceneView.currentDrawingSceneView != null &&
                       SceneView.currentDrawingSceneView == EditorWindow.focusedWindow;
            }
        }
#endif

        public static bool FastEquals(this Matrix4x4 m1, Matrix4x4 m2)
        {
            return m1.m00 == m2.m00 && m1.m01 == m2.m01 && m1.m02 == m2.m02 && m1.m03 == m2.m03 &&
                   m1.m10 == m2.m10 && m1.m11 == m2.m11 && m1.m12 == m2.m12 && m1.m13 == m2.m13 &&
                   m1.m20 == m2.m20 && m1.m21 == m2.m21 && m1.m22 == m2.m22 && m1.m23 == m2.m23 &&
                   m1.m30 == m2.m30 && m1.m31 == m2.m31 && m1.m32 == m2.m32 && m1.m33 == m2.m33;
        }

        public static bool Equals(this Color32 col1, Color32 col2)
        {
            return col1.r == col2.r && col1.g == col2.g && col1.b == col2.b && col1.a == col2.a;
        }
    }

    internal class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        private Func<T, T, bool> distinct;
        private Func<T, int> hash;

        public GenericEqualityComparer(Func<T, T, bool> distinct, Func<T, int> hash)
        {
            this.distinct = distinct;
            this.hash = hash;
        }

        public bool Equals(T x, T y)
        {
            if (System.Object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (System.Object.ReferenceEquals(x, null) ||
                System.Object.ReferenceEquals(y, null))
            {
                return false;
            }
            return distinct(x, y);
        }

        public int GetHashCode(T obj)
        {
            return hash(obj);
        }
    }

    internal class GenericComparer<T> : IComparer<T>
    {
        private Func<T, T, int> comparer;

        public GenericComparer(Func<T, T, int> comparer)
        {
            this.comparer = comparer;
        }

        public int Compare(T x, T y)
        {
            return comparer(x, y);
        }
    }

    public class ReadOnlyAttribute : PropertyAttribute
    {

    }
}