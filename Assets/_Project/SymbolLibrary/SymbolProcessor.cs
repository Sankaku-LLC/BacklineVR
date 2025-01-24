using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace CurseVR.SymbolSystem
{
    /// <summary>
    /// This class handles unary operations on the strokeData primitive, such as serialization. Methods are static
    /// 
    /// Entry 1 of stroke end vectors will be the starting position of the first stroke. This is doable because the first end vector is undefined, and only the first stroke's start position is necessary to extend to other stroke starting positions.
    /// </summary>
    public static class SymbolProcessor
    {
        public const int MAXRES = 32;
        public const int MAXLEVEL = 5;
        public const int MINPOINTS = MAXRES / 8;


        /// <summary>
        /// Takes in raw stroke data (each stroke being an N-dimensional vector specified in dimensions). If the stroke can be fixed by doubling points, do that.
        /// </summary>
        /// <param name="rawStrokes"></param>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        public static SymbolData ProcessStrokes(List<StrokeCapture> rawStrokes, int dimensions)
        {
            var strokes = new List<List<Stroke>>();// Res -> Segment number -> stroke data  (More efficient, minimize cache misses (testing is by resolution))
            var basisTranslations = new List<Vector>(rawStrokes.Count - 1);
            var basisRotations = new List<Vector>(rawStrokes.Count - 1);
            List<Vector> strokeEndVectors = new List<Vector>();
            List<Vector> strokeEndDeltas = new List<Vector>();
            List<float> strokeTotalLengths = new List<float>();
            List<List<float>> strokeLengths = new List<List<float>>();
            if (rawStrokes.Count == 0)
            {
                return null;
            }

            for (int i = rawStrokes.Count - 1; i >= 0; i--)
            {
                var stroke = rawStrokes[i].Stroke;
                if(i != 0)
                {
                    var positionDiff = rawStrokes[i].CameraLocalPosition - rawStrokes[i - 1].CameraLocalPosition;
                    basisTranslations.Insert(0,new Vector(positionDiff));
                    var rotationDiff = rawStrokes[i].CameraLocalRotation * Quaternion.Inverse(rawStrokes[i - 1].CameraLocalRotation);
                    basisRotations.Insert(0,new Vector(rotationDiff));
                }

                ///NOTE ON MODIFICATION: Made it so it will keep doubling points until the number of vertices is >= MAXRES
                if (stroke.Count >= MINPOINTS)
                {
                    while (stroke.Count < MAXRES)
                    {

                        DoublePoints(stroke);
                    }
                }
                else
                {
                    Debug.Log("Too small to quadrouple -> How was this allowed to processor?");
                }

            }

            for (int i = 0; i <= MAXLEVEL; i++)
            {
                strokes.Add(new List<Stroke>());
                strokeTotalLengths.Add(0);
            }
            //this code does extra work and renders some of the later steps unnecessary, but will ensure that too-large-gap issues do not occur at any resolution
            for (int strokeIndex = 0; strokeIndex < rawStrokes.Count; strokeIndex++)
            {
                int totalStrokeVertices = rawStrokes[strokeIndex].Stroke.Count;
                Vector previous = rawStrokes[strokeIndex].Stroke[0];
                Vector current;
                //Preprocess: Record distances along path per vertex, and also calculate total distance
                float totalDistance = 0;
                for (int vectorSet = 1; vectorSet < rawStrokes[strokeIndex].Stroke.Count; vectorSet++)
                {
                    current = rawStrokes[strokeIndex].Stroke[vectorSet];

                    float currentDistance = Vector.Distance(previous, current);

                    totalDistance += currentDistance;
                    previous = current;
                }

                float maxGap = totalDistance / MAXRES;

                int iterations = 0;
                for (int vectorSet = 1; vectorSet < rawStrokes[strokeIndex].Stroke.Count; vectorSet++)
                {
                    Vector curren = rawStrokes[strokeIndex].Stroke[vectorSet];
                    Vector prev = rawStrokes[strokeIndex].Stroke[vectorSet - 1];
                    float currentDistance = Vector.Distance(prev, curren);


                    if (currentDistance > maxGap)
                    {
                        var step = prev + maxGap * .5f * (curren - prev).Normalized();

                        rawStrokes[strokeIndex].Stroke.Insert(vectorSet, step);

                        if (iterations > 5000)
                        {
                            Debug.LogError("After");
                            Debug.LogError(prev[0] + " " + prev[1] + " " + prev[2] + "\n" + step[0] + " " + step[1] + " " + step[2] + "\n" + curren[0] + " " + curren[1] + " " + curren[2]);
                            Debug.LogError(currentDistance + " " + maxGap);
                        }
                    }

                    iterations++;

                    if (iterations > 5010)
                    {
                        Debug.LogError("WHY IS IT LOOPING THIS MUCH? " + currentDistance + " " + maxGap + " " + totalStrokeVertices);
                        return null;
                    }
                }

            }

            Vector curStart;
            Vector prevEnd = new Vector(new float[dimensions]);


            //Populate stroke lengths array, res-> strokes -> lengths
            for (int i = 0; i <= MAXLEVEL; i++)
            {
                List<float> strokeLengthRes = new List<float>();
                for (int k = 0; k < rawStrokes.Count; k++)
                {
                    strokeLengthRes.Add(0);
                }
                strokeLengths.Add(strokeLengthRes);
            }




            for (int strokeIndex = 0; strokeIndex < rawStrokes.Count; strokeIndex++)
            {
                int totalStrokeVertices = rawStrokes[strokeIndex].Stroke.Count;
                Vector previous = curStart = rawStrokes[strokeIndex].Stroke[0];
                Vector current;
                float[] distances = new float[totalStrokeVertices];
                //Preprocess: Record distances along path per vertex, and also calculate total distance
                float totalDistance = 0;
                for (int vectorSet = 1; vectorSet < rawStrokes[strokeIndex].Stroke.Count; vectorSet++)
                {
                    current = rawStrokes[strokeIndex].Stroke[vectorSet];

                    float currentDistance = Vector.Distance(previous, current);

                    totalDistance += currentDistance;
                    distances[vectorSet] = totalDistance;
                    //Debug.Log(distances[vectorSet]);
                    previous = current;
                }
                strokeEndDeltas.Add(previous - curStart);//Difference between current vector start and current vector end (previous is set to that after loop)
                strokeEndVectors.Add(curStart - prevEnd);//Difference between previous end and current vector start
                prevEnd = previous;


                int[] centers = GetCenters(distances);


                var stroke = new Stroke();
                for (int i = 0; i <= MAXLEVEL; i++)
                {
                    int steps = 1 << i;
                    int increment = MAXRES / steps;

                    float currentLength = 0;
                    for (int j = 0; j <= MAXRES - increment; j += increment)
                    {
                        var point1 = rawStrokes[strokeIndex].Stroke[centers[j]];
                        var point2 = rawStrokes[strokeIndex].Stroke[centers[j + increment]];
                        var diff = point2 - point1;
                        //Used to debug NaN error
                        var length = diff.Magnitude();
                        if (Mathf.Approximately(length, 0))
                        {
                            Debug.LogError(centers[j] + " " + centers[j + increment] + " " + j + " " + (j + increment) + " " + totalStrokeVertices);
                            return null;
                        }
                        stroke.Add(diff);
                        currentLength += length;
                    }
                    strokeLengths[i][strokeIndex] = currentLength;
                    strokes[i].Add(stroke);
                    strokeTotalLengths[i] += currentLength;


                    stroke = new Stroke();

                }
                //include inter-stroke gap delta (unweighted power addition)
                if (strokeIndex + 1 < rawStrokes.Count)
                {
                    //upcoming end vector
                    Vector delta = rawStrokes[strokeIndex + 1].Stroke[0] - prevEnd;
                    float length = delta.Magnitude();

                    for (int k = 0; k <= MAXLEVEL; k++)
                    {
                        strokeTotalLengths[k] += length;
                    }
                }
            }

            float strokeNumEntropy = CalcStrokeNumEntropy(rawStrokes.Count);
            SymbolData sd = new SymbolData();
            sd.StrokeEndVectors = strokeEndVectors;
            sd.Strokes = strokes;
            sd.StrokeEndDeltas = strokeEndDeltas;
            sd.Dimensions = dimensions;
            sd.StrokeCount = rawStrokes.Count;
            sd.StrokeTotalLengths = strokeTotalLengths;
            sd.StrokeLengths = strokeLengths;
            sd.Complexity = CalcSymbolDataEntropy(sd);
            sd.RecordCount = 1;
            sd.BasisTranslations = basisTranslations;
            sd.BasisRotations = basisRotations;
            UpdateCentroid(sd);
            return sd;
        }
        public static void DoublePoints(Stroke stroke)
        {
            var strokePoints = stroke.Points;
            int finalCount = 2 * stroke.Count - 1;
            for (int i = 1; i < finalCount; i++)
            {
                var previous = stroke[i - 1];
                var current = stroke[i];
                var halfway = 0.5f * (current - previous) + previous;
                stroke.Insert(i, halfway);
                i++;
            }
        }
        //Will return the indices of the stroke points closest to each 1/MAXRES length of the stroke
        public static int[] GetCenters(float[] distances)
        {
            int[] results = new int[MAXRES + 1];
            float increment = distances[distances.Length - 1] / MAXRES;
            float currentProgress = 0;
            int resultCount = 0;
            for (int i = 0; i < distances.Length - 1; i++)
            {
                float prevDiff = distances[i] - currentProgress;
                float curDiff = distances[i + 1] - currentProgress;

                //Debug.Log(resultCount + " " + currentProgress + " " + distances[i] + " " + distances[i + 1]);

                //if inbetween
                if (prevDiff <= 0 && curDiff >= 0)
                {
                    if (-prevDiff < curDiff)
                    {
                        results[resultCount] = i;
                    }
                    else
                    {
                        results[resultCount] = i + 1;
                    }
                    resultCount++;
                    currentProgress += increment;
                    if (resultCount == MAXRES)
                    {
                        break;
                    }
                }
            }
            results[MAXRES] = distances.Length - 1;
            return results;
        }

        public static float CalcStrokeNumEntropy(int segNum)
        {
            float sum = 0;
            //Expanded calculation of ln(x!)
            //Default entropy calc is -sum(p * ln p)
            //Since p is same always, the sum and first p go away. since p = 1/x!, the negative goes away, leaving ln(x!)
            for (int i = 1; i <= segNum; i++)
            {
                sum += Mathf.Log(i);
            }
            return sum;
        }
        public static float CalcSymbolDataEntropy(SymbolData symbolData)
        {
            float changeInLengthSum = 0;
            float changeInDirectionSum = 0;
            for (int j = 0; j < symbolData.StrokeCount; j++)
            {
                float prevLength = 0;
                float currentLength = 0;
                Vector maxResLastPointBeforeEnd = new Vector(new float[3]);
                maxResLastPointBeforeEnd.AddThis(symbolData.Strokes[MAXLEVEL][j][0]);//Set up with first displacement, since loop skips first entry
                for (int i = 0; i <= MAXLEVEL; i++)
                {
                    //Why am I using 1 - 1/count ?
                    currentLength = symbolData.StrokeLengths[i][j];
                    if (prevLength != 0)
                    {
                        changeInLengthSum += Mathf.Abs(prevLength - currentLength) * (1f / symbolData.Strokes[i][j].Count);
                    }
                    prevLength = currentLength;

                    //will get the smaller angle, which means it can also be negative

                    for (int k = 1; k < symbolData.Strokes[i][j].Count; k++)
                    {
                        //Why is this here?
                        changeInDirectionSum += Vector.AngleBetween(symbolData.Strokes[i][j][k - 1], symbolData.Strokes[i][j][k]) / Mathf.PI * (1f / symbolData.Strokes[i][j].Count);
                        maxResLastPointBeforeEnd.AddThis(symbolData.Strokes[MAXLEVEL][j][k]);
                        if (float.IsNaN(changeInDirectionSum))
                        {
                            Debug.LogError("NaN Change In Direction detected!");
                            return -1;
                        }
                    }

                }
                //include inter-stroke gap delta (unweighted power addition)

                if (j + 1 < symbolData.StrokeCount)
                {
                    //upcoming end vector
                    Vector delta = symbolData.StrokeEndVectors[j + 1];

                    float length = delta.Magnitude();
                    changeInLengthSum += Mathf.Abs(length - prevLength) / symbolData.StrokeCount;
                    prevLength = length;

                    //get the direction of the end of the path
                    var endOfPathDir = maxResLastPointBeforeEnd - symbolData.StrokeEndDeltas[j];// it should really be the other way around, idk why it ends up negative that way. This works so that's fine
                                                                                                //returns the change in angle between two vectors
                    changeInDirectionSum += Vector.AngleBetween(delta, endOfPathDir) / Mathf.PI / symbolData.StrokeCount;
                }
            }
            float strokeNumEntropy = CalcStrokeNumEntropy(symbolData.StrokeCount);
            //Debug.Log("Segment Count: " + strokeNumEntropy +" Length: " + changeInLengthSum + " Direction: " + changeInDirectionSum);
            //To prevent a bunch of little strokes from being better than one long, complex one,
            return strokeNumEntropy + changeInLengthSum + changeInDirectionSum;//Add instead of multiply
        }
        public static float CalculateEntropy(SymbolData data)
        {
            return CalcSymbolDataEntropy(data);
        }
        public static void UpdateCentroid(SymbolData source)
        {
            List<Vector> endVectors = source.StrokeEndVectors;
            Vector3 spawnPoint = new Vector3(endVectors[0][0], endVectors[0][1], endVectors[0][2]);

            var data = source.Strokes[MAXLEVEL];

            Vector3 currentPosition = spawnPoint;
            Vector3 currentStartPosition = currentPosition;
            //Matrix4x4 cameraMatrix = Camera.main.transform.localToWorldMatrix;
            Vector3 viewerLocalPosition = Vector3.zero;
            Quaternion viewerLocalRotation = Quaternion.identity;

            Vector3 sumPositions = Vector3.zero;
            int vertexCount = 0;

            for (int j = source.BasisRotations.Count - 1; j >= 0; j--)
            {
                Vector3 translation = new Vector3(source.BasisTranslations[j][0], source.BasisTranslations[j][1], source.BasisTranslations[j][2]);
                viewerLocalPosition -= translation;
                Quaternion deltaRotation = new Quaternion(source.BasisRotations[j][0],
    source.BasisRotations[j][1],
    source.BasisRotations[j][2],
    source.BasisRotations[j][3]);
                viewerLocalRotation = Quaternion.Inverse(deltaRotation) * viewerLocalRotation;
            }

            for (int j = 0; j < data.Count; j++)
            {
                List<Vector3> positions = new List<Vector3>();
                for (int k = 0; k < data[j].Count; k++)
                {
                    Vector3 deltaTranslation = new Vector3(data[j][k][0], data[j][k][1], data[j][k][2]);
                    sumPositions += viewerLocalRotation * currentPosition + viewerLocalPosition;
                    vertexCount++;
                    currentPosition += deltaTranslation;
                }

                Vector3 endPoint = currentStartPosition + new Vector3(source.StrokeEndDeltas[j][0], source.StrokeEndDeltas[j][1], source.StrokeEndDeltas[j][2]);
                sumPositions += viewerLocalRotation * endPoint + viewerLocalPosition;
                vertexCount++;

                if (j + 1 < source.StrokeCount)
                {
                    currentPosition = currentStartPosition = endPoint + new Vector3(endVectors[j + 1][0], endVectors[j + 1][1], endVectors[j + 1][2]);//add vector from this to next stroke start
                }
                //Apply the transform at the end of each stroke being drawn
                if (j < data.Count - 1)
                {

                    Vector3 translation = new Vector3(source.BasisTranslations[j][0], source.BasisTranslations[j][1], source.BasisTranslations[j][2]);
                    Quaternion deltaRotation = new Quaternion(source.BasisRotations[j][0],
    source.BasisRotations[j][1],
    source.BasisRotations[j][2],
    source.BasisRotations[j][3]);
                    viewerLocalRotation = deltaRotation * viewerLocalRotation;
                    viewerLocalPosition += translation;
                }
            }
            sumPositions /= vertexCount;
            source.Centroid = new float[] { sumPositions.x, sumPositions.y, sumPositions.z };
        }
    }
}