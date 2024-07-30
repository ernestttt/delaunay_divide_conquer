using UnityEngine;
using System;

namespace MeshSlicing.Math{
    
    public static class Math2d
    {
        private const float TOLERANCE = 0.000001f;

        private static bool FindIntersectionPointBetweenLines2D(
            Vector2 l11, 
            Vector2 l12, 
            Vector2 l21, 
            Vector2 l22, 
            out Vector2 p
            )
        {
            p = Vector2.zero;

            float denom = (l22.y - l21.y) * (l12.x - l11.x) - (l22.x - l21.x) * (l12.y - l11.y);

            if (System.Math.Abs(denom) < TOLERANCE)
            {
                return false;
            }

            float num = (l22.x - l21.x) * (l11.y - l21.y) - (l22.y - l21.y) * (l11.x - l21.x);
            float t = num / denom;

            p = l11 + t * (l12 - l11);

            return true;
        }

        public static bool FindLineSegmentIntersection(Vector2 l1, Vector2 l2, Vector2 s1, Vector2 s2, out Vector2 p)
        {
            if(FindIntersectionPointBetweenLines2D(l1, l2, s1, s2, out p)
                && !IsThePointOnEndsOfSegment(p, s1, s2) 
                && IsThePointOnSegment(p, s1, s2))
            {
                return true;
            }

            return false;
        }

        public static bool IsVectorsCollinear(Vector2 v1, Vector2 v2){
            float cross2d = v1.x * v2.y - v1.y * v2.x;
            bool isCollinear = System.Math.Abs(cross2d) < TOLERANCE;
            
            return isCollinear;
        }

        public static bool IsThePointOnSegment(Vector2 p, Vector2 s1, Vector2 s2)
        {
            Vector2 s1p = p - s1;
            Vector2 s1s2 = s2 - s1;

            if(!IsVectorsCollinear(s1p, s1s2))
            {
                return false;
            }

            float dotS1PS1S2 = Vector2.Dot(s1p, s1s2);
            float dotS1S2 = Vector2.Dot(s1s2, s1s2);

            if (dotS1PS1S2 > 0 && dotS1PS1S2 < dotS1S2)
            {
                return true;
            }

            return false;
        }

        public static bool IsThePointOnEndsOfSegment(Vector2 p, Vector2 s1, Vector2 s2){
            return p == s1 || p == s2;
        }

        private static bool IsIn01Segment(float value){
            return value > 0 && value < 1.0;
        }

        private static Vector2 FindMiddlePoint2D(Vector2 a, Vector2 b)
        {
            return a + 0.5f * (b - a);
        }

        private static Vector2 FindPerpendicularForLineOnPoint2D(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ba = b - a;
            Vector2 result = new Vector2(-ba.y, ba.x) + p;
            return result;
        }

        public static bool IsPointInsideCircle2D(Vector2 point, Vector2 circleCenter, float radius)
        {
            float squareRadius = radius * radius;
            float squareDistance = (point - circleCenter).sqrMagnitude;

            return squareDistance <= squareRadius;
        }

        public static bool IsPointsCollinear(Vector2 p1, Vector2 p2, Vector2 p3){
            Vector2 p21 = p2 - p1;
            Vector2 p31 = p3 - p1;

            float collinearValue = p21.x * p31.y - p31.x * p21.y;
            return System.Math.Abs(collinearValue) < 1e-6;
            //return collinearValue == 0;
        }

        public static (Vector2 center, float radius) GetCircumferenceAroundTriangle(
            Vector2 t1,
            Vector2 t2,
            Vector2 t3)
        {
            Vector2 middle1 = FindMiddlePoint2D(t1, t2);
            Vector2 middle2 = FindMiddlePoint2D(t2, t3);

            Vector2 perpendicular1 = FindPerpendicularForLineOnPoint2D(t1, t2, middle1);
            Vector2 perpendicular2 = FindPerpendicularForLineOnPoint2D(t2, t3, middle2);

            Vector2 center;
            if (FindIntersectionPointBetweenLines2D(
                middle1, perpendicular1, middle2, perpendicular2, out center
            ))
            {
                float radius = (center - t1).magnitude;
                return (center, radius);
            }
            else
            {
                throw new Exception("Bad triangle");
            }
        }

        public static bool IsPointInTriangCircum(Vector2 p, Vector2 t1, Vector2 t2, Vector2 t3){
            (Vector2, float) circumference = GetCircumferenceAroundTriangle(t1, t2, t3);
            return IsPointInsideCircle2D(p, circumference.Item1, circumference.Item2);
        }

        public static bool IsShortestLineBetweenSegments(Vector2 s1, Vector2 s2, Vector2 t1, Vector2 t2, out Vector2 result1, out Vector2 result2)  {
            result1 = Vector2.zero;
            result2 = Vector2.zero;

            Vector2 p1 = s1;
            Vector2 p2 = s2;
            Vector2 p3 = t1;
            Vector2 p4 = t2;
            Vector2 p13 = p1 - p3;
            Vector2 p43 = p4 - p3;

            if (p43.sqrMagnitude < float.Epsilon)
            {
                return false;
            }
            Vector2 p21 = p2 - p1;
            if (p21.sqrMagnitude < float.Epsilon)
            {
                return false;
            }

            float d1343 = p13.x * p43.x + p13.y * p43.y;
            float d4321 = p43.x * p21.x + p43.y * p21.y;
            float d1321 = p13.x * p21.x + p13.y * p21.y;
            float d4343 = p43.x * p43.x + p43.y * p43.y;
            float d2121 = p21.x * p21.x + p21.y * p21.y;

            float denom = d2121 * d4343 - d4321 * d4321;
            if (Mathf.Abs(denom) < Mathf.Epsilon)
            {
                return false;
            }
            float numer = d1343 * d4321 - d1321 * d4343;

            float mua = numer / denom;
            float mub = (d1343 + d4321 * (mua)) / d4343;

            result1.x = p1.x + mua * p21.x;
            result1.y = p1.y + mua * p21.y;
            result1.x = p3.x + mub * p43.x;
            result1.y = p3.y + mub * p43.y;

            return true;
        }
    }








}
