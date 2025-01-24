
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CurseVR.SymbolSystem
{
    [Serializable]
    public class SymbolData
    {
        public string Name = "";
        public List<Vector> StrokeEndVectors = new List<Vector>();//first entry is always the start point.
        public List<List<Stroke>> Strokes = new List<List<Stroke>>();
        public List<Vector> StrokeEndDeltas = new List<Vector>();//How much the end of the segment is offset from the head (A translation invariant alternative to endpoints)
        public int Dimensions;
        public int RecordCount;
        public float Complexity;
        public int StrokeCount;
        public List<float> StrokeTotalLengths = new List<float>();//per resolution total, for good comparison in diffs and merges. Adds scale invariance?
        public List<List<float>> StrokeLengths = new List<List<float>>();
        public float[] Centroid; //Used for placement. Set by highest res centroid, updated with res segments.
        public List<Vector> BasisTranslations = new List<Vector>();//List of camera relative displacements at time of casting
        public List<Vector> BasisRotations = new List<Vector>();//List of camera relative rotations at time of casting
        /// <summary>
        /// A field used by applications to determine what this symbol should do internally
        /// </summary>
        public string CustomData = "";
    }
    public interface IStrokeProvider
    {
        public Stroke EndStroke();
        public void Destroy(Transform center, float destructionTime);
        public void SetPosition(Vector3 position);
    }
    public class StrokeCapture//Raw data of a stroke used to process into SymbolData
    {
        public float StartTime;
        public float EndTime;
        public Stroke Stroke;
        public Vector3 CameraLocalPosition;
        public Quaternion CameraLocalRotation;
        public IStrokeProvider StrokeProvider;
        public StrokeCapture(float startTime, IStrokeProvider strokeProvider)
        {
            StartTime = startTime;
            StrokeProvider = strokeProvider;
        }
        public StrokeCapture(StrokeCapture toClone)
        {
            StartTime= toClone.StartTime;
            EndTime= toClone.EndTime;
            Stroke = toClone.Stroke;
            CameraLocalPosition = toClone.CameraLocalPosition;
            CameraLocalRotation = toClone.CameraLocalRotation;
            StrokeProvider = toClone.StrokeProvider;
        }
        public void EndStroke(float endTime, Vector3 cameraPos, Quaternion cameraRot)
        {
            EndTime = endTime;
            Stroke = StrokeProvider.EndStroke();
            CameraLocalPosition = cameraPos;
            CameraLocalRotation = cameraRot;
        }
    }
    [Serializable]
    public class Stroke: IEnumerable<Vector>
    {
        private List<Vector> _strokePoints;
        public Stroke(List<Vector3> points)
        {
            _strokePoints = new List<Vector>(points.Count);
            foreach(var point in points)
            {
                _strokePoints.Add(new Vector(point));
            }
        }
        public Stroke(List<Vector> points)
        {
            _strokePoints = new List<Vector>(points.Count);
            _strokePoints.AddRange(points);
        }
        public Stroke()
        {
            _strokePoints = new List<Vector>();
        }
        public int Count => _strokePoints.Count;
        public List<Vector> Points => _strokePoints;
        public IEnumerator<Vector> GetEnumerator()
        {
            return _strokePoints.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public Vector this[int i]
        {
            get { return _strokePoints[i]; }
            set { _strokePoints[i] = value; }
        }
        public void Insert(int idx, Vector point)
        {
            _strokePoints.Insert(idx, point);
        }
        public void Add(Vector point)
        {
            _strokePoints.Add(point);
        }
    }
    [Serializable]
    public class Vector
    {
        private float[] _data;
        public readonly int Dimensions;
        public Vector(params float[] data)
        {
            _data = data;
            Dimensions = data.Length;
        }
        public Vector(Vector3 data)
        {
            _data = new float[3] { data.x, data.y, data.z };
            Dimensions = 3;
        }
        public Vector(Quaternion data)
        {
            _data = new float[4] { data.x, data.y, data.z, data.w };
            Dimensions = 4;
        }
        public float x
        {
            get
            {
                return _data[0];
            }
            set
            {
                _data[0] = value;
            }
        }
        public float y
        {
            get
            {
                return _data[1];
            }
            set
            {
                _data[1] = value;
            }
        }
        public float z
        {
            get
            {
                return _data[2];
            }
            set
            {
                _data[2] = value;
            }
        }
        public float w
        {
            get
            {
                return _data[3];
            }
            set
            {
                _data[3] = value;
            }
        }

        public float this[int i]
        {
            get { return _data[i]; }
            set { _data[i] = value; }
        }
        public static Vector operator +(Vector a, Vector b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new InvalidOperationException("Input vectors have different dimensions: " + a.Dimensions + " " + b.Dimensions);

            var result = new float[a.Dimensions];
            for (int i = 0; i < a.Dimensions; i++)
            {
                result[i] = a[i] + b[i];
            }
            return new Vector(result);
        }
        public static Vector operator -(Vector a, Vector b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new InvalidOperationException("Input vectors have different dimensions: " + a.Dimensions + " " + b.Dimensions);

            var result = new float[a.Dimensions];
            for (int i = 0; i < a.Dimensions; i++)
            {
                result[i] = a[i] - b[i];
            }
            return new Vector(result);
        }
        public static Vector operator *(float scalar, Vector a)
        {
            var result = new float[a.Dimensions];

            for (int i = 0; i < a.Dimensions; i++)
            {
                result[i] = scalar * a[i];
            }
            return new Vector(result);
        }
        public static float operator *(Vector a, Vector b)
        {
            if (a.Dimensions != b.Dimensions)
                throw new InvalidOperationException("Input vectors have different dimensions: " + a.Dimensions + " " + b.Dimensions);
            float sum = 0;
            for (int i = 0; i < a.Dimensions; i++)
            {
                sum += a[i] * b[i];
            }
            return sum;
        }

        public static explicit operator Vector3(Vector v) => new Vector3(v[0], v[1], v[2]);
        public static explicit operator Quaternion(Vector v) => new Quaternion(v[0], v[1], v[2], v[3]);

        public static float Distance(Vector a, Vector b)
        {
            return (a - b).Magnitude();
        }
        public float Magnitude()
        {
            return Mathf.Sqrt(this * this);
        }
        public Vector Normalized()
        {
            float length = Magnitude();
            return 1f / length * this;
        }
        public static Vector AverageRotation(Vector source, Vector sample, int sourcePool)
        {
            float[] results = new float[4];
            Quaternion from = new Quaternion(source[0], source[1], source[2], source[3]);
            Quaternion to = new Quaternion(sample[0], sample[1], sample[2], sample[2]);
            float calculatedT = 1f / (sourcePool + 1);
            Quaternion result = Quaternion.Slerp(from, to, calculatedT);
            results[0] = result.x;
            results[1] = result.y;
            results[2] = result.z;
            results[3] = result.w;
            return new Vector(results);
        }
        public void AddThis(Vector v2)
        {
            for (int i = 0; i < Dimensions; i++)
            {
                _data[i] += v2[i];
            }
        }
        public void ScaleThis(float scale)
        {
            for (int i = 0; i < Dimensions; i++)
            {
                _data[i] *= scale;
            }
        }
        public void SubtractThis(Vector v2)
        {
            for (int i = 0; i < Dimensions; i++)
            {
                _data[i] -= v2[i];
            }
        }
        public static float AngleBetween(Vector v1, Vector v2)
        {
            //Debug.Log(Mathf.Acos(dot(normalize(v1), normalize(v2))) + " " + Mathf.Acos(2 * Mathf.Clamp01((dot(normalize(v1), normalize(v2)) + 1) / 2) - 1));
            return Mathf.Acos(2 * Mathf.Clamp01((v1.Normalized() * v2.Normalized() + 1) / 2) - 1);
        }
        public override string ToString()
        {
            return string.Format("<{0},{1},{2}>", x, y, z);
        }
    }
}