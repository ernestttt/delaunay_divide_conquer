using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MeshSlicing.Delaunay
{
    public class Vertex2d
    {
        private Vector2 _pos;
        private int _index;

        public Vector2 Pos => _pos;
        public int Index => _index;

        private List<Edge2d> _edges = new List<Edge2d>();

        public List<Edge2d> Edges => _edges;

        public Vertex2d(int index, Vector2 pos)
        {
            _pos = pos;
            _index = index;
        }

        public void AddEdge(Edge2d edge)
        {
            if (!_edges.Contains(edge))
            {
                _edges.Add(edge);
            }
        }

        public List<Vertex2d> GetOrderedPotentials(Edge2d edge)
        {
            Vector2 edgeVector = edge.GetVectorFromEdge(this);
            Vector2 ortogonal = edge.GetOrtogonal();
            List<Vertex2d> orderedPotentials =
                _edges.Where(a => 
                {
                    double dot = Vector2.Dot(a.GetVectorFromEdge(this), ortogonal);
                    return dot > 1E-06d;
                })
                .OrderBy(a => Vector2.Angle(a.GetVectorFromEdge(this), edgeVector))
                .Select(a => a.GetAnotherVertex(this))
                .Where(a => a != edge.Vertex1 && a != edge.Vertex2)
                .ToList();

            return orderedPotentials;
        }

        public List<Edge2d> GetOrderedEdgesByAngle(Edge2d edge)
        {
            Vector2 edgeVector = edge.GetVectorFromEdge(this);
            List<Edge2d> orderedEdges = _edges.OrderBy(a =>
            {
                Vector2 e = a.GetVectorFromEdge(this);
                return Vector2.Angle(e, edgeVector);
            }).ToList();

            return orderedEdges;
        }

        public bool IsConvex(Edge2d edge)
        {
            Vector2 edgeVector = edge.GetVectorFromEdge(this);
            Vector2 ortogonal = edge.GetOrtogonal();

            return !_edges.Any(a => 
            {
                double angle = Vector2.Angle(a.GetVectorFromEdge(this), ortogonal);
                return angle > 90.0001;
            });
        }

        public void RemoveEdge(Edge2d edge)
        {
            _edges.Remove(edge);
        }

        public override string ToString()
        {
            return $"{_index}";
        }
    }
}