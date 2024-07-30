using UnityEngine;
using MeshSlicing.Math;
using System.Linq;
using System.Collections.Generic;

namespace MeshSlicing.Delaunay
{
    public class Edge2d
    {
        private Vertex2d _vertex1;
        private Vertex2d _vertex2;

        public Vertex2d Vertex1 => _vertex1;
        public Vertex2d Vertex2 => _vertex2;

        public float SqrLenght => (_vertex2.Pos - _vertex1.Pos).sqrMagnitude;

        private float TOLERANCE = 0.000001f;

        public Edge2d(Vertex2d vertex1, Vertex2d vertex2)
        {
            _vertex1 = vertex1;
            _vertex2 = vertex2;

            _vertex1.AddEdge(this);
            _vertex2.AddEdge(this);
        }

        public void InitVertices(){
            _vertex1.AddEdge(this);
            _vertex2.AddEdge(this);
        }

        public Vertex2d GetAnotherVertex(Vertex2d vertex)
        {
            return _vertex1 == vertex ? _vertex2 : _vertex1;
        }

        public bool IsTheSameEdge(Vertex2d vertex1, Vertex2d vertex2)
        {
            bool isSame =
                (vertex1 == _vertex1 && vertex2 == _vertex2) ||
                (vertex1 == _vertex2 && vertex2 == _vertex1);

            return isSame;
        }

        public Vector2 GetVectorFromEdge(Vertex2d vertex)
        {
            Vector2 result = _vertex2.Pos - _vertex1.Pos;
            if (vertex == _vertex2)
            {
                result = -result;
            }

            return result;
        }

        public Vector2 GetVectorFromEdge(){
            return GetVectorFromEdge(_vertex1);
        }

        // refactor this method
        public Vector2 GetOrtogonal()
        {
            Vector2 vector = GetVectorFromEdge(_vertex1);
            Vector2 vector1 = new Vector2(-vector.y, vector.x);
            Vector2 vector2 = new Vector2(vector.y, -vector.x);

            for (int i = 0; i < Vertex1.Edges.Count; i++)
            {
                Edge2d edge1 = Vertex1.Edges[i];
                if (edge1 == this)
                {
                    continue;
                }
                for (int j = 0; j < Vertex2.Edges.Count; j++)
                {
                    Edge2d edge2 = Vertex2.Edges[j];
                    if (edge2 == this)
                    {
                        continue;
                    }

                    if (edge1.HasSameVertexWith(edge2, out Vertex2d vertex))
                    {
                        edge1.HasSameVertexWith(this, out Vertex2d ourVertex);
                        Vector2 edge1Vector = edge1.GetVectorFromEdge(ourVertex);

                        if (Vector2.Dot(edge1Vector, vector1) < 0)
                        {
                            return vector1;
                        }
                        else
                        {
                            return vector2;
                        }
                    }
                }
            }

            float sumDot1 = 0;
            float sumDot2 = 0;

            // for first vertex
            for (int i = 0; i < Vertex1.Edges.Count; i++)
            {
                Edge2d edge = Vertex1.Edges[i];
                if (edge == this)
                {
                    continue;
                }

                sumDot1 += Vector2.Dot(edge.GetVectorFromEdge(_vertex1), vector1);
                sumDot2 += Vector2.Dot(edge.GetVectorFromEdge(_vertex1), vector2);
            }

            for (int i = 0; i < Vertex2.Edges.Count; i++)
            {
                Edge2d edge = Vertex2.Edges[i];
                if (edge == this)
                {
                    continue;
                }

                sumDot1 += Vector2.Dot(edge.GetVectorFromEdge(_vertex2), vector1);
                sumDot2 += Vector2.Dot(edge.GetVectorFromEdge(_vertex2), vector2);
            }

            if (sumDot1 > sumDot2)
            {
                return vector1;
            }
            else
            {
                return vector2;
            }
        }

        public void ClearVertices()
        {
            _vertex1.RemoveEdge(this);
            _vertex2.RemoveEdge(this);
        }

        public bool IsCrossedByLine(Vector2 l1, Vector2 l2)
        {
            if (Math2d.FindLineSegmentIntersection(l1, l2, _vertex1.Pos, _vertex2.Pos, out Vector2 ignore))
            {
                return true;
            }

            return false;
        }

        public void DebugVisualize()
        {
            Debug.DrawLine((Vector2)_vertex1.Pos, (Vector2)_vertex2.Pos, Color.green);
        }

        public bool IsConvex()
        {
            // it must be the same sign
            // it may be any orthogonal, not necessary with higher y
            Vector2 orthogonal = GetOrtogonal();
            // Get all vector edges for first Vertex
            List<Vector2> edgesVectors1 = Vertex1.Edges.Where(a => a != this).Select(a => a.GetVectorFromEdge(Vertex1)).ToList();
            List<Vector2> edgesVectors2 = Vertex2.Edges.Where(a => a != this).Select(a => a.GetVectorFromEdge(Vertex2)).ToList();
            
            Vector2 vector1 = _vertex2.Pos - _vertex1.Pos;
            Vector2 vector2 = _vertex1.Pos - _vertex2.Pos;

            float dotSum1 = 0;
            float absDotSum1 = 0;

            for (int i = 0; i < edgesVectors1.Count; i++)
            {
                float dot = Vector2.Dot(orthogonal, edgesVectors1[i]);
                if(Mathf.Abs(dot) < 1E-06f){
                    dot = 0;
                    float withVector1Dot = Vector2.Dot(vector1, edgesVectors1[i]);
                    if(withVector1Dot > 0){
                        // overlapping
                        return false;
                    }
                }
                dotSum1 += dot;
                absDotSum1 += Mathf.Abs(dot);
            }
            
            if(Mathf.Abs(Mathf.Abs(dotSum1) -absDotSum1) > 1E-06d)
            {
                return false;
            }

            float absDotSum2 = 0;
            float dotSum2 = 0;

            for(int i = 0; i < edgesVectors2.Count; i++){
                float dot = Vector2.Dot(orthogonal, edgesVectors2[i]);
                if(Mathf.Abs(dot) < 1E-06d)
                {
                    dot = 0;
                    float withVector2ot = Vector2.Dot(vector2, edgesVectors2[i]);
                    if(withVector2ot > 0){
                        // overlapping
                        return false;
                    }
                }
                dotSum2 += dot;
                absDotSum2 += Mathf.Abs(dot);
            }

            if(Mathf.Abs(Mathf.Abs(dotSum2) - absDotSum2) > 1E-06d)
            {
                return false;
            }

            bool isConvex = (dotSum1 >= 0 && dotSum2 >= 0) || (dotSum1 <= 0 && dotSum2 <= 0);
            return isConvex;
        }

        public bool IsAnyVertexInsideEdge(List<Vertex2d> points){
            for(int i = 0; i < points.Count; i++){
                if(points[i] == _vertex1 || points[i] == _vertex2){
                    continue;
                }
                if(IsPointInsideEdge(new Vector2(points[i].Pos.x, points[i].Pos.y))){
                    return true;
                }
            }

            return false;
        }

        private bool IsPointInsideEdge(Vector2 point){
            return Math2d.IsThePointOnSegment(point, _vertex1.Pos, _vertex2.Pos);
        }

        public bool HasSameVertexWith(Edge2d edge, out Vertex2d vertex)
        {

            vertex = null;
            bool sameVertex = true;
            if (_vertex1 == edge._vertex1)
            {
                vertex = _vertex1;
            }
            else if (_vertex2 == edge._vertex2)
            {
                vertex = _vertex2;
            }
            else if (_vertex1 == edge._vertex2)
            {
                vertex = _vertex1;
            }
            else if (_vertex2 == edge._vertex1)
            {
                vertex = _vertex2;
            }
            else
            {
                sameVertex = false;
            }

            return sameVertex;
        }

        public override string ToString()
        {
            return $"{_vertex1.Index} - {_vertex2.Index}";
        }
    }
}