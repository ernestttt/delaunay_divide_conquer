using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;
using MeshSlicing.Math;
using UnityEngine;

namespace MeshSlicing.Delaunay
{
    public class SubMesh2d
    {
        public List<Vertex2d> _vertices;
        public List<Edge2d> _edges;
        public List<Triangle2d> _triangles;

        private bool _nestStep = false;
        private bool _isAnimated = false;

        public int[] Triangles
        {
            get
            {
                List<int> indices = new List<int>();
                for (int i = 0; i < _triangles.Count; i++)
                {
                    indices.AddRange(_triangles[i].Indices);
                }

                return indices.ToArray();
            }
        }

        public SubMesh2d(Vector2[] points)
        {
            _edges = new List<Edge2d>();
            _triangles = new List<Triangle2d>();
            Vector2 anchorPoint = points.OrderBy(a => a.x).ThenBy(a => a.y).First();
            var customComparer = Comparer<float>.Create((a, b) => {
                float diff = a - b;
                if (Mathf.Abs(diff) < 1E-06d){
                    return 0;
                }

                return a > b ? 1 : -1;
            });

            _vertices = CreateVertices(points).OrderBy(a => a.Pos.x, customComparer).
                        ThenBy(a => a.Pos.y, customComparer).ToList();
        }

        private static List<Vertex2d> CreateVertices(Vector2[] points)
        {
            List<Vertex2d> vertices = new List<Vertex2d>();
            for (int i = 0; i < points.Length; i++)
            {
                vertices.Add(new Vertex2d(i, points[i]));
            }

            return vertices;
        }

        public async UniTask StartTriangulation(bool isAnimated = false)
        {
            _edges.Clear();
            _isAnimated = isAnimated;
            await DivideAndConquer(_vertices);
            PickOutTriangles();
            CheckForAbnormalTriangles();
        }

        public void CheckForAbnormalTriangles(){
            for (int i = 0; i < _triangles.Count; i++)
            {
                Triangle2d triangle = _triangles[i];
                if (triangle.IsAbnormal())
                {
                    Debug.LogError("Abnormal triangle");
                }
            }
        }

        public void NextStep()
        {
            _nestStep = true;
        }

        public void DebugVisualize()
        {
            foreach (var edge in _edges)
            {
                edge.DebugVisualize();
            }
        }

        public void PickOutTriangles()
        {
            _triangles.Clear();

            // triple cyrcles))
            for (int i = 0; i < _edges.Count; i++)
            {
                List<Vertex2d> commonVertices = new List<Vertex2d>();
                Edge2d edge = _edges[i];
                List<Edge2d> _vertex1Edges = edge.Vertex1.GetOrderedEdgesByAngle(edge);
                List<Edge2d> _vertex2Edges = edge.Vertex2.GetOrderedEdgesByAngle(edge);


                for (int j = 0; j < _vertex1Edges.Count; j++)
                {
                    Edge2d edge1 = _vertex1Edges[j];
                    if (edge1 == edge)
                    {
                        continue;
                    }

                    for (int k = 0; k < _vertex2Edges.Count; k++)
                    {
                        Edge2d edge2 = _vertex2Edges[k];
                        if (edge2 == edge)
                        {
                            continue;
                        }

                        if (edge1.HasSameVertexWith(edge2, out Vertex2d commonVertex))
                        {
                            var circle =
                                Math2d.GetCircumferenceAroundTriangle(commonVertex.Pos, edge.Vertex1.Pos, edge.Vertex2.Pos);

                            if (!commonVertices.Any(a => Math2d.IsPointInsideCircle2D(a.Pos, circle.center, circle.radius)))
                            {
                                AddTriangle(edge, edge1, edge2);
                            }
                            commonVertices.Add(commonVertex);
                            break;
                        }
                    }
                }
            }
        }

        private void RemoveAbnormalTriangles()
        {
            for(int i = 0; i < _triangles.Count; i++){
                Triangle2d triangle = _triangles[i];
                if(triangle.IsAbnormal()){
                    Debug.Log("Abnormal triangle");
                    _triangles[i] = null;
                }
            }

            _triangles.RemoveAll(a => a == null);
        }


        public void DebugInfo()
        {
            Debug.Log($"vertices {_vertices.Count}/ edges {_edges.Count}/ triangles {_triangles.Count}");
        }

        private void AddTriangle(Edge2d edge1, Edge2d edge2, Edge2d edge3)
        {
            if (_triangles.Any(a => a.IsSameTriangle(edge1, edge2, edge3))) return;

            _triangles.Add(new Triangle2d(edge1, edge2, edge3));
        }

        private async UniTask DivideAndConquer(List<Vertex2d> vertices)
        {
            if (vertices.Count > 3)
            {
                int halfLength = vertices.Count / 2;
                List<Vertex2d> leftVertices = vertices.GetRange(0, halfLength);
                List<Vertex2d> rightVertices = vertices.GetRange(halfLength, vertices.Count - halfLength);

                await DivideAndConquer(leftVertices);
                await DivideAndConquer(rightVertices);

                await WaitNextStep();
                Edge2d baseLR = GetLRBaseEdge(leftVertices, rightVertices);
                _edges.Add(baseLR);
                Debug.Log("Add base edge " + baseLR);
                await WaitNextStep();
                await MergeWithRL(baseLR);
            }
            else if (vertices.Count == 3)
            {
                await WaitNextStep();
                AddEdges(vertices);
            }
            else if (vertices.Count == 2)
            {
                await WaitNextStep();
                AddEdge(vertices);
            }
        }

        private async UniTask MergeWithRL(Edge2d baseRL)
        {
            if(baseRL == null)
            {

            }
            bool isFirstCandidate = TryToFindCandidate(baseRL, baseRL.Vertex1, out Vertex2d candidate1);
            bool isSecondCandidate = TryToFindCandidate(baseRL, baseRL.Vertex2, out Vertex2d candidate2);

            Vertex2d newBaseV1;
            Vertex2d newBaseV2;

            if (isFirstCandidate && isSecondCandidate)
            {
                if (Math2d.IsPointInTriangCircum(
                    candidate2.Pos,
                    candidate1.Pos,
                    baseRL.Vertex1.Pos,
                    baseRL.Vertex2.Pos))
                {
                    newBaseV1 = baseRL.Vertex1;
                    newBaseV2 = candidate2;
                }
                else
                {
                    newBaseV1 = candidate1;
                    newBaseV2 = baseRL.Vertex2;
                }
            }
            else if (isFirstCandidate)
            {
                newBaseV1 = candidate1;
                newBaseV2 = baseRL.Vertex2;
            }
            else if (isSecondCandidate)
            {
                newBaseV1 = baseRL.Vertex1;
                newBaseV2 = candidate2;
            }
            else
            {
                return;
            }

            Edge2d newBaseRL = new Edge2d(newBaseV1, newBaseV2);
            _edges.Add(newBaseRL);

            await WaitNextStep();

            await MergeWithRL(newBaseRL);
        }

        private async UniTask WaitNextStep()
        {
            if (!_isAnimated) return;

            await UniTask.WaitUntil(() => _nestStep);
            _nestStep = false;
        }

        private bool TryToFindCandidate(Edge2d edge, Vertex2d vertex, out Vertex2d candidate)
        {
            Vector2 vertex1Pos = edge.Vertex1.Pos;
            Vector2 vertex2Pos = edge.Vertex2.Pos;

            candidate = null;

            List<Vertex2d> orderedPotentials = vertex.GetOrderedPotentials(edge);

            if (orderedPotentials.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < orderedPotentials.Count - 1; i++)
            {
                Vertex2d potential = orderedPotentials[i];
                Vertex2d nextpotential = orderedPotentials[i + 1];

                Vector2 potPos = potential.Pos;
                Vector2 nextPos = nextpotential.Pos;

                if (Math2d.IsPointInTriangCircum(nextPos, vertex1Pos, vertex2Pos, potPos))
                {
                    RemoveEdge(vertex, potential);
                }
                else
                {
                    candidate = potential;
                    return true;
                }
            }

            candidate = orderedPotentials.Last();
            return true;
        }

        private void RemoveEdge(Vertex2d v1, Vertex2d v2)
        {
            Edge2d edge = _edges.First(a => a.IsTheSameEdge(v1, v2));
            RemoveEdge(edge);
        }

        private void RemoveEdge(Edge2d edge)
        {
            edge.ClearVertices();
            _edges.Remove(edge);
        }

        private Edge2d GetLRBaseEdge(List<Vertex2d> left, List<Vertex2d> right)
        {
            Vertex2d v1 = null;
            Vertex2d v2 = null;

            // start from left
            for (int i = 0; i < left.Count; i++)
            {
            // than by right
                for (int j = 0; j < right.Count; j++)
                {
                    v1 = left[i];
                    v2 = right[j];

                    // generate edge and add
                    Edge2d convexEdge = new Edge2d(v1, v2);
                    // check is convex
                    if (convexEdge.IsConvex())
                    {
                        return convexEdge;
                    }
                    else{
                        convexEdge.ClearVertices();
                    }
                }
            }

            return null;
        }

        private bool IsLineCrossAnyEdges(Vector2 l1, Vector2 l2)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                Edge2d edge = _edges[i];
                if (edge.IsCrossedByLine(l1, l2))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddEdges(List<Vertex2d> vertices)
        {
            Vertex2d vertex1 = vertices[0];
            Vertex2d vertex2 = vertices[1];
            Vertex2d vertex3 = vertices[2];

            Edge2d edge1;
            Edge2d edge2;
            Edge2d edge3;

            bool isCollinear = false;

            if (Math2d.IsPointsCollinear(vertex1.Pos, vertex2.Pos, vertex3.Pos))
            {
                isCollinear = true;
            }

            if (!TryToGetEdge(vertex1, vertex2, out edge1))
            {
                edge1 = AddEdge(vertex1, vertex2);
            }

            if (!TryToGetEdge(vertex2, vertex3, out edge2))
            {
                edge2 = AddEdge(vertex2, vertex3);
            }

            if (!TryToGetEdge(vertex3, vertex1, out edge3))
            {
                edge3 = AddEdge(vertex3, vertex1);
            }

            if (isCollinear)
            {
                Edge2d[] edges = new Edge2d[] { edge1, edge2, edge3 };
                Edge2d longestEdge = edges.OrderByDescending(a => a.SqrLenght).First();
                RemoveEdge(longestEdge);
                Debug.Log("colinear points remove edge " + longestEdge);
            }
        }

        private void AddEdge(List<Vertex2d> vertices)
        {
            if (_edges.Any(a => a.IsTheSameEdge(vertices[0], vertices[1])))
            {
                Debug.Log("same edge");
            }
            Edge2d edge = new Edge2d(vertices[0], vertices[1]);
            // Debug.Log("Add edge " + edge);
            _edges.Add(edge);
        }

        private Edge2d AddEdge(Vertex2d vertex1, Vertex2d vertex2)
        {
            Edge2d edge = new Edge2d(vertex1, vertex2);
            _edges.Add(edge);
            //Debug.Log("Add edge " + edge);
            return edge;
        }

        private bool TryToGetEdge(Vertex2d vertex1, Vertex2d vertex2, out Edge2d edge)
        {
            edge = _edges.FirstOrDefault(a => a.IsTheSameEdge(vertex1, vertex2));

            if (edge != null)
            {
                return true;
            }

            return false;
        }
    }
}