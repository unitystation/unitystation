using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    public enum VLSDebugMode
    {
        None = 0,
        Geometry = 1,
        Bounds = 2,
        Raycasting = 4,

        All = Geometry | Bounds | Raycasting
    }

    [System.Serializable]
    public class VLSUtility 
    {       
        private static List<VLSEdge> visibleEdges = new List<VLSEdge>();
        private static Vector3 kPoint;//, rayOffset = Vector3.zero;
        private static Vector3 lightPosition;
        private static Transform lightTransform;
        private static VLSRayHit rHit = new VLSRayHit();
        private static Vector3 target = Vector3.zero;
        private static bool hit = false;
                
        public static void GenerateDirectionalMesh(VLSLight _light, LayerMask _layerMask)
        {
            _light.Buffer.Clear();
            CollectVisibleEdges(_light, _layerMask);
        }
        
        //private static Vector3 zOffset = new Vector3(0, 0, 0);
        public static void GenerateRadialMesh(VLSLight _light, LayerMask _layerMask, float _obstPenetration = 0)
        {
                lightTransform = _light.transform;
                lightPosition = lightTransform.position;
                //zOffset.Set(0, 0, lightPosition.z);

                _light.Buffer.Clear();

                CollectVisibleEdges(_light, _layerMask);
                visibleEdges.AddRange(_light.edges);

#if UNITY_EDITOR
                if (VLSDebug.IsModeActive(VLSDebugMode.Geometry))
                {
                    for (int e = 0; e < visibleEdges.Count; e++)
                    {
                        if (VLSDebug.IsModeActive(VLSDebugMode.Raycasting))
                        {
                            Debug.DrawLine(visibleEdges[e].PointA.position, visibleEdges[e].PointB.position, Color.magenta);

                            if (visibleEdges[e].IsEnd)
                                Debug.DrawRay(visibleEdges[e].PointB.position, visibleEdges[e].Normal * 0.2f, Color.red);

                            if (visibleEdges[e].IsStart)
                                Debug.DrawRay(visibleEdges[e].PointA.position, visibleEdges[e].Normal * 0.2f, Color.green);
                        }
                    }
                }
#endif

                target.Set(0, 0, 0);
                hit = false;
                for (int e = 0; e < visibleEdges.Count; e++)
                {                    
                    for (int e2 = 0; e2 < visibleEdges.Count; e2++)
                    {
                        if (LineIntersects(visibleEdges[e].PointA.position, visibleEdges[e].PointB.position, visibleEdges[e2].PointA.position, visibleEdges[e2].PointB.position, ref kPoint))
                        {
                            LineCast(lightPosition, kPoint, ref rHit);
                            _light.Buffer.AddPoint(lightTransform.InverseTransformPoint(rHit.point), Mathf.Atan2(rHit.point.y - lightPosition.y, rHit.point.x - lightPosition.x));
                        }
                    }

                    if (_light.bounds.Contains(visibleEdges[e].PointA.position))
                    {
                        if (visibleEdges[e].IsStart)
                        {
                            target = visibleEdges[e].PointA.position - (visibleEdges[e].Direction * 0.001f);
                            hit = LineCast(lightPosition, target, ref rHit);
                            _light.Buffer.AddPoint(lightTransform.InverseTransformPoint(rHit.point), Mathf.Atan2(visibleEdges[e].PointA.position.y - lightPosition.y, visibleEdges[e].PointA.position.x - lightPosition.x));

                            if (!hit)
                            {
                                RayCast(lightPosition, target, ref rHit);
                                _light.Buffer.AddPoint(lightTransform.InverseTransformPoint(rHit.point), Mathf.Atan2(rHit.point.y - lightPosition.y, rHit.point.x - lightPosition.x));
                            }
                        }
                        else
                        {
                            target = visibleEdges[e].PointA.position;
                            LineCast(lightPosition, target, ref rHit);
                            _light.Buffer.AddPoint(lightTransform.InverseTransformPoint(rHit.point), Mathf.Atan2(visibleEdges[e].PointA.position.y - lightPosition.y, visibleEdges[e].PointA.position.x - lightPosition.x));
                        }
                    }

                    if (_light.bounds.Contains(visibleEdges[e].PointB.position))
                    {
                        if (visibleEdges[e].IsEnd)
                        {
                            target = visibleEdges[e].PointB.position + (visibleEdges[e].Direction * 0.001f);
                            hit = LineCast(lightPosition, target, ref rHit);
                            _light.Buffer.AddPoint(lightTransform.InverseTransformPoint(rHit.point), Mathf.Atan2(visibleEdges[e].PointB.position.y - lightPosition.y, visibleEdges[e].PointB.position.x - lightPosition.x));

                            if (!hit)
                            {
                                RayCast(lightPosition, target, ref rHit);
                                _light.Buffer.AddPoint(lightTransform.InverseTransformPoint(rHit.point), Mathf.Atan2(rHit.point.y - lightPosition.y, rHit.point.x - lightPosition.x));
                            }
                        }
                    }
                }

                _light.Buffer.vertices.Sort();
                _light.Buffer.AddPoint(Vector3.zero, 0);
        }
        
        private static void CollectVisibleEdges(VLSLight _light, LayerMask _layerMask)
        {
            visibleEdges.Clear();

            #region Collect Radial Edges
            if (_light is VLSRadial || _light is VLSRadialCS)
            {
                for(int o = 0; o < VLSViewer.VisibleObstructions.Count; o++)//VLSObstructor obst in VLSViewer.VisibleObstructions)
                {
                    if (VLSViewer.VisibleObstructions[o] == null)
                        continue;

                    if ((_layerMask.value & (1 << VLSViewer.VisibleObstructions[o].gameObject.layer)) == 0)
                        continue;

                    if (VLSViewer.VisibleObstructions[o].bounds.Overlaps(_light.bounds, true))
                    {
                        bool prevEdgeVisible = false;
                        for (int i = 0; i < VLSViewer.VisibleObstructions[o].edges.Count; i++)
                        {
                            VLSViewer.VisibleObstructions[o].edges[i].IsStart = false;
                            VLSViewer.VisibleObstructions[o].edges[i].IsEnd = false;

                            if (QuickDot(VLSViewer.VisibleObstructions[o].edges[i].PointA.position - _light.transform.position, VLSViewer.VisibleObstructions[o].edges[i].Normal) <= 0)
                            {
                                if (!prevEdgeVisible)
                                    VLSViewer.VisibleObstructions[o].edges[i].IsStart = true;

                                prevEdgeVisible = true;
                                visibleEdges.Add(VLSViewer.VisibleObstructions[o].edges[i]);
                            }
                            else if (prevEdgeVisible)
                            {
                                VLSViewer.VisibleObstructions[o].edges[i - 1].IsEnd = true;
                                prevEdgeVisible = false;
                            }
                        }

                        if (VLSViewer.VisibleObstructions[o].edges[0].IsEnd && VLSViewer.VisibleObstructions[o].edges[VLSViewer.VisibleObstructions[o].edges.Count - 1].IsStart)
                        {
                            VLSViewer.VisibleObstructions[o].edges[0].IsStart = false;
                            VLSViewer.VisibleObstructions[o].edges[VLSViewer.VisibleObstructions[o].edges.Count - 1].IsEnd = false;
                        }
                        else
                        {
                            VLSViewer.VisibleObstructions[o].edges[VLSViewer.VisibleObstructions[o].edges.Count - 1].IsEnd = true;
                        }
                    }
                }
            }
            #endregion

            #region Collect Directional Edges
            if(_light is VLSDirectional)
            {
                foreach (VLSObstructor obst in VLSViewer.VisibleObstructions)
                {
                    if ((_layerMask.value & (1 << obst.gameObject.layer)) == 0)
                        continue;

                    if (obst.bounds.Overlaps(_light.bounds))
                    {
                        bool prevEdgeVisible = false;
                        for (int i = 0; i < obst.edges.Count; i++)
                        {
                            obst.edges[i].IsStart = false;
                            obst.edges[i].IsEnd = false;

                            if (QuickDot(-_light.transform.up, obst.edges[i].Normal) < 0)
                            {
                                if (VLSDebug.IsModeActive(VLSDebugMode.Raycasting))
                                    Debug.DrawLine(obst.edges[i].PointA.position, obst.edges[i].PointB.position, Color.red);

                                if (!prevEdgeVisible)
                                    obst.edges[i].IsStart = true;

                                prevEdgeVisible = true;
                                visibleEdges.Add(obst.edges[i]);
                            }
                            else if (prevEdgeVisible)
                            {
                                obst.edges[i - 1].IsEnd = true;
                                prevEdgeVisible = false;
                            }
                        }

                        if (obst.edges[0].IsEnd && obst.edges[obst.edges.Count - 1].IsStart)
                        {
                            obst.edges[0].IsStart = false;
                            obst.edges[obst.edges.Count - 1].IsEnd = false;
                        }
                        else
                        {
                            obst.edges[obst.edges.Count - 1].IsEnd = true;
                        }
                    }
                }
            }
            #endregion
        }

        #region Raycasters/Linecasters
        public static bool RayCast(Vector3 _origin, Vector3 _towards, ref VLSRayHit _hitInfo)
        {
            return LineCast(_origin, _origin + (_towards - _origin) * 1000 , ref _hitInfo);
        }

        private static RaycastHit2D rHit2D;
        private static Vector3 lc_outPnt = new Vector3();
        private static float lc_curDist = 0;
        private static bool lc_Intersects = false;
        public static bool LineCast(Vector3 _origin, Vector3 _end, ref VLSRayHit _hitInfo)
        {
            //rHit2D = Physics2D.Linecast(_origin, _end);

            //if (rHit2D.collider != null)
            //{
            //    _hitInfo.point = rHit2D.point;                
            //    return true;
            //}
            //else
            //{
            //    _hitInfo.point = _end;
            //    return false;
            //}

                lc_outPnt.Set(0, 0, 0);// = Vector3.zero;
                _hitInfo.sqrDist = Mathf.Infinity;
                lc_curDist = 0;
                lc_Intersects = false;

#if UNITY_EDITOR
                if (VLSDebug.IsModeActive(VLSDebugMode.Raycasting))
                    Debug.DrawLine(_origin, _end, new Color(.2f, .2f, .2f, .2f));
#endif

                for (int e = 0; e < visibleEdges.Count; e++)
                {
                    if (LineIntersects(visibleEdges[e].PointA.position, visibleEdges[e].PointB.position, _origin, _end, ref lc_outPnt))
                    {
                        lc_curDist = Vector3.SqrMagnitude(lc_outPnt - _origin);

                        if (lc_curDist < _hitInfo.sqrDist)
                        {
                            _hitInfo.sqrDist = lc_curDist;
                            _hitInfo.point.Set(lc_outPnt.x, lc_outPnt.y, lc_outPnt.z);// +(Vector3)(-edge.Normal);
                            //_hitInfo.obstructor = edge.Parent as VLSObstructor;
                            lc_Intersects = true;
                        }
                    }
                }

                if (!lc_Intersects)
                {
                    _hitInfo.point = _end;
                    //_hitInfo.obstructor = null;
                }

                //_hitInfo.direction = (_hitInfo.point - _origin).normalized;

#if UNITY_EDITOR
                if (VLSDebug.IsModeActive(VLSDebugMode.Raycasting))
                    Debug.DrawLine(_origin, _hitInfo.point, new Color(.8f, .8f, .2f, .8f));
#endif
                
            return lc_Intersects;
        }

        private static Vector3 li_a, li_b, li_c;
        private static float li_u, li_t, li_dp;
        public static bool LineIntersects(Vector3 _a1, Vector3 _a2, Vector3 _b1, Vector3 _b2, ref Vector3 _out)
        {
            li_a = _a2 - _a1;
            li_c = _b2 - _b1;
            li_dp = li_a.x * li_c.y - li_a.y * li_c.x;

            if (li_dp <= 0)
                return false;

            li_b = _b1 - _a1;
            li_t = (li_b.x * li_c.y - li_b.y * li_c.x) / li_dp;
            if (li_t < 0 || li_t > 1)
                return false;

            li_u = (li_b.x * li_a.y - li_b.y * li_a.x) / li_dp;
            if (li_u < 0 || li_u > 1)
                return false;

            _out.Set(_a1.x + li_t * li_a.x, _a1.y + li_t * li_a.y, 0);

            return true;
        }
        #endregion

        #region Math Helpers
        //private static Vector3 RayOffset(Vector3 _origin, Vector3 _point)
        //{
        //    rayOffset.Set(_point.y - _origin.y, -(_point.x - _origin.x), 0);
        //    return rayOffset.normalized * 0.0005f;
        //}

        private bool SegmentInFrontOf(VLSEdge a, VLSEdge b, Vector2 relativeTo)
        {
            // NOTE: we slightly shorten the segments so that
            // intersections of the endpoints (common) don't count as
            // intersections in this algorithm                        

            bool a1 = LeftOf(a.PointA.position, a.PointA.position, Interpolate(b.PointA.position, b.PointB.position, 0.01f));
            bool a2 = LeftOf(a.PointB.position, a.PointA.position, Interpolate(b.PointB.position, b.PointA.position, 0.01f));
            bool a3 = LeftOf(a.PointB.position, a.PointA.position, relativeTo);

            bool b1 = LeftOf(b.PointB.position, b.PointA.position, Interpolate(a.PointA.position, a.PointB.position, 0.01f));
            bool b2 = LeftOf(b.PointB.position, b.PointA.position, Interpolate(a.PointB.position, a.PointA.position, 0.01f));
            bool b3 = LeftOf(b.PointB.position, b.PointA.position, relativeTo);

            // NOTE: this algorithm is probably worthy of a short article
            // but for now, draw it on paper to see how it works. Consider
            // the line A1-A2. If both B1 and B2 are on one side and
            // relativeTo is on the other side, then A is in between the
            // viewer and B. We can do the same with B1-B2: if A1 and A2
            // are on one side, and relativeTo is on the other side, then
            // B is in between the viewer and A.
            if (b1 == b2 && b2 != b3) return true;
            if (a1 == a2 && a2 == a3) return true;
            if (a1 == a2 && a2 != a3) return false;
            if (b1 == b2 && b2 == b3) return false;

            // If A1 != A2 and B1 != B2 then we have an intersection.
            // Expose it for the GUI to show a message. A more robust
            // implementation would split segments at intersections so
            // that part of the segment is in front and part is behind.

            //demo_intersectionsDetected.push([a.p1, a.p2, b.p1, b.p2]);
            return false;

            // NOTE: previous implementation was a.d < b.d. That's simpler
            // but trouble when the segments are of dissimilar sizes. If
            // you're on a grid and the segments are similarly sized, then
            // using distance will be a simpler and faster implementation.
        }

        public static bool LeftOf(Vector2 p1, Vector2 p2, Vector2 point)
        {
            float cross = (p2.x - p1.x) * (point.y - p1.y)
                        - (p2.y - p1.y) * (point.x - p1.x);

            return cross < 0;
        }

        public static Vector2 Interpolate(Vector2 p, Vector2 q, float f)
        {
            return new Vector2(p.x * (1.0f - f) + q.x * f, p.y * (1.0f - f) + q.y * f);
        }

        public static bool PointInsideWedge(Vector2 _center, Vector2 _p1, Vector2 _p2, Vector2 _testPoint)
        {
            return (_testPoint.y * (_p1.x - _center.x) - _testPoint.x * (_p1.y - _center.y)) * (_testPoint.y * (_p2.x - _center.x) - _testPoint.x * (_p2.y - _center.y)) < 0;
        }

        public static float QuickDot(Vector2 _lhs, Vector2 _rhs)
        {
            return (_lhs.x * _rhs.x) + (_lhs.y * _rhs.y);
        }
        #endregion
    }
}