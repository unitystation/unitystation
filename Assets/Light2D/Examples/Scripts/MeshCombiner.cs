using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D.Examples
{
    /// <summary>
    /// Combines child mesh renderers into one mesh at startup.
    /// It gives much better performance for small meshes than StaticBatchingUtility.Combine.
    /// </summary>
    public class MeshCombiner : MonoBehaviour
    {
        private bool _isDone = false;

        void OnEnable()
        {
            if (!_isDone)
            {
                StopCoroutine("Combine");
                StartCoroutine(Combine());
            }
        }

        private IEnumerator Combine()
        {
            yield return null;
            var groups = GetComponentsInChildren<Renderer>()
                .Where(r => r.GetComponent<MeshRenderer>() != null && r.GetComponent<MeshFilter>() != null &&
                     r.GetComponent<MeshFilter>().sharedMesh != null)
                .GroupBy(r => new GroupKey
                {
                    Material = r.sharedMaterial,
                    SortingOrder = r.sortingOrder,
                    Layer = r.gameObject.layer
                })
                .ToList();

            var vertices = new List<Vector3>(1024);
            var triangles = new List<int>(1024);
            var uv0 = new List<Vector2>(1024);
            var uv1 = new List<Vector2>(1024);
            var colors = new List<Color>(1024);
            var tangents = new List<Vector4>(1024);

            foreach (var group in groups)
            {
                var obj = new GameObject("Combined Mesh");
                obj.transform.parent = transform;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;
                obj.layer = group.Key.Layer;

                var meshRenderer = obj.AddComponent<MeshRenderer>();
                meshRenderer.material = (Material)Instantiate(group.Key.Material);
                meshRenderer.sortingOrder = group.Key.SortingOrder;

                var firstMesh = group.First().GetComponent<MeshFilter>().mesh;

                bool useUv0 = firstMesh.uv != null && firstMesh.uv.Length != 0;
                bool useUv1 = firstMesh.uv2 != null && firstMesh.uv2.Length != 0;
                bool useColors = firstMesh.colors != null && firstMesh.colors.Length != 0;

                vertices.Clear();
                triangles.Clear();
                tangents.Clear();

                if (useUv0) uv0.Clear();
                if (useUv1) uv1.Clear();
                if (useColors) colors.Clear();

                foreach (var meshPart in group)
                {
                    var filter = meshPart.GetComponent<MeshFilter>();
                    var smallMesh = filter.mesh;
                    var startVertexIndex = vertices.Count;
                    vertices.AddRange(smallMesh.vertices.Select(v => filter.transform.TransformPoint(v)));
                    triangles.AddRange(smallMesh.triangles.Select(t => t + startVertexIndex));

                    var localTangents = smallMesh.tangents == null || smallMesh.tangents.Length == 0
                        ? Enumerable.Repeat(new Vector4(1, 0), smallMesh.vertexCount)
                        : smallMesh.tangents;
                    tangents.AddRange(localTangents.Select(t => (Vector4)filter.transform.TransformVector(t)));

                    if (useUv0) uv0.AddRange(smallMesh.uv);
                    if (useUv1) uv1.AddRange(smallMesh.uv2);
                    if (useColors) colors.AddRange(smallMesh.colors);

                    Destroy(meshPart);
                    Destroy(filter);
                    var customSprite = meshPart.GetComponent<CustomSprite>();
                    if (customSprite != null)
                        Destroy(customSprite);
                }

                var meshFilter = obj.AddComponent<MeshFilter>();
                var mesh = meshFilter.mesh = new Mesh();

                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.tangents = tangents.ToArray();
                if (useUv0) mesh.uv = uv0.ToArray();
                if (useUv1) mesh.uv2 = uv1.ToArray();
                if (useColors) mesh.colors = colors.ToArray();
            }

            _isDone = true;
        }

        struct GroupKey : IEquatable<GroupKey>
        {
            public Material Material;
            public int SortingOrder;
            public int Layer;

            public bool Equals(GroupKey other)
            {
                return Equals(Material, other.Material) && SortingOrder == other.SortingOrder && Layer == other.Layer;
            }

            private sealed class MaterialSortingOrderLayerEqualityComparer : IEqualityComparer<GroupKey>
            {
                public bool Equals(GroupKey x, GroupKey y)
                {
                    return Equals(x.Material, y.Material) && x.SortingOrder == y.SortingOrder && x.Layer == y.Layer;
                }

                public int GetHashCode(GroupKey obj)
                {
                    unchecked
                    {
                        int hashCode = (obj.Material != null ? obj.Material.GetHashCode() : 0);
                        hashCode = (hashCode*397) ^ obj.SortingOrder;
                        hashCode = (hashCode*397) ^ obj.Layer;
                        return hashCode;
                    }
                }
            }

            private static readonly IEqualityComparer<GroupKey> MaterialSortingOrderLayerComparerInstance = new MaterialSortingOrderLayerEqualityComparer();

            public static IEqualityComparer<GroupKey> MaterialSortingOrderLayerComparer
            {
                get { return MaterialSortingOrderLayerComparerInstance; }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is GroupKey && Equals((GroupKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (Material != null ? Material.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ SortingOrder;
                    hashCode = (hashCode*397) ^ Layer;
                    return hashCode;
                }
            }

            public static bool operator ==(GroupKey left, GroupKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(GroupKey left, GroupKey right)
            {
                return !left.Equals(right);
            }
        }
    }
}
