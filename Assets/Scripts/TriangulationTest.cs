using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MeshSlicing.Delaunay;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using Cysharp.Threading.Tasks;

public class TriangulationTest : MonoBehaviour
{
    [SerializeField] private bool _isAnimated;
    [SerializeField] private bool _isUpdateByKey;
    [SerializeField] private float _animationInterval = 1f;
    [SerializeField] private TextAsset _pointsAsset;
    [SerializeField, Range(0, 100)] private int _pointsIndex = 0;
    [SerializeField, Range(0, 360)] private float _rotation = 0f;
    [SerializeField] private bool _isGenerateRandomPoints = false;
    [SerializeField] private int _numberOfPoints = 3;
    [SerializeField] private float _range = 1;
    [SerializeField] private bool _showNumberOfPoints = false;

    private List<Vector2[]> _pointsList = new List<Vector2[]>();
    private Vector2[] _activePoints;


    private SubMesh2d _subMesh2D = null;
    private int[] _triangles;

    private float _nextUpdateTime = float.MinValue;

    private async void Start(){
        ParsePointsAsset(); 
        Triangualate();
    }

    private void OnValidate() {
        
        if (_pointsList != null && _pointsIndex < _pointsList.Count)
        {
            Triangualate();
        }
    }

    private Vector2[] GenerateRandomPoints(){
        Vector2[] result = new Vector2[_numberOfPoints];

        for(int i = 0; i < _numberOfPoints; i++){
            result[i] = new Vector2(Random.Range(-_range, _range), Random.Range(-_range, _range));
        }

        return result;
    }

    private async void Triangualate(){
        _activePoints = RotatePoints(_pointsList[_pointsIndex]);

        if (_isGenerateRandomPoints)
        {
            _activePoints = GenerateRandomPoints();
        }

        _subMesh2D = new SubMesh2d(_activePoints.Select(a => (Vector2)a).ToArray());
        await UniTask.Delay(1000);
        await _subMesh2D.StartTriangulation(_isAnimated);
        _triangles = _subMesh2D.Triangles;
        string edgesString = string.Join(", ", _subMesh2D._edges.Select(a => a.ToString()).ToArray());
        Debug.Log($"End triangulation, triangles count is {_triangles.Length / 3}, \n edges is {edgesString}");
    }

    private Vector2[] RotatePoints(Vector2[] points){
        List<Vector2> pointsList = new List<Vector2>();
        for(int i = 0; i < points.Length; i++){
            pointsList.Add(Quaternion.AngleAxis(_rotation, -Vector3.forward) * (Vector3)points[i]);
        }
        return pointsList.ToArray();
    }

    private void ParsePointsAsset(){
        string text = _pointsAsset.text;
        string[] lines = text.Split("///", StringSplitOptions.RemoveEmptyEntries);

        for(int i = 0; i < lines.Length; i++){
            string[] vectorLines = lines[i].Trim().Split("\n", StringSplitOptions.RemoveEmptyEntries);
            Vector2[] points = new Vector2[vectorLines.Length];
            for(int j = 0; j < vectorLines.Length; j++){
                string[] xy = vectorLines[j].Split(",");
                Vector2 vector = new Vector2(float.Parse(xy[0]), float.Parse(xy[1]));
                points[j] = vector;
            }

            // set to the origin
            for (int j = 1; j < points.Length; j++){
                points[j] -= points[0];
            }
            points[0] = Vector2.zero;

            _pointsList.Add(points);
        }
    }

    private void Update(){
        if(_subMesh2D != null){
            _subMesh2D.DebugVisualize();
        }

        if(_subMesh2D != null && _isAnimated 
            && (!_isUpdateByKey && _nextUpdateTime < Time.time 
                || (_isUpdateByKey && Input.GetKeyUp(KeyCode.Space))))
        {
            _subMesh2D.NextStep();
            _nextUpdateTime = Time.time + _animationInterval;
        }
    }


    private void OnDrawGizmos() {
        if(_subMesh2D != null){
            Gizmos.color = Color.red;
            for(int i = 0; i < _subMesh2D._vertices.Count; i++)
            {
                Vector2 point = (Vector2)_subMesh2D._vertices[i].Pos;
                Gizmos.DrawSphere(point, 0.03f);
                if(_showNumberOfPoints){
                    Handles.Label(point, $"{_subMesh2D._vertices[i].Index}");
                }
            }
        }
    }
}
