using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CurseVR.SymbolSystem
{
    /// <summary>
    /// This class is responsible for binary operations, such as training and testing
    /// </summary>
    public static class SymbolOperator
    {
        public static void Merge(SymbolData source, SymbolData sample)
        {
            float gapAmount = 0;

            var weight = 1f / (source.RecordCount + 1);
            for (int i = 0; i < sample.StrokeEndVectors.Count; i++)
            {
                source.StrokeEndVectors[i] = weight * (source.RecordCount * source.StrokeEndVectors[i] + sample.StrokeEndVectors[i]);
                source.StrokeEndDeltas[i] = weight * (source.RecordCount * source.StrokeEndDeltas[i]+ sample.StrokeEndDeltas[i]);
                gapAmount += source.StrokeEndVectors[i].Magnitude();
            }
            for(int i=0; i< source.BasisTranslations.Count; i++)
            {
                source.BasisTranslations[i] = weight * (source.RecordCount * source.BasisTranslations[i] + sample.BasisTranslations[i]);
                source.BasisRotations[i] = Vector.AverageRotation(source.BasisRotations[i], sample.BasisRotations[i], source.RecordCount);
            }
            var sampleStrokes = sample.Strokes;
            List<float> distances = new List<float>(); //lengths per resolution
            List<List<float>> strokeLengths = new List<List<float>>();

            for (int i = 0; i <= SymbolProcessor.MAXLEVEL; i++)
            {
                distances.Add(gapAmount);
                strokeLengths.Add(new List<float>());
            }

            for (int h = 0; h < sampleStrokes.Count; h++)//represents the resolutions, in order
            {
                float currentLength = 0;
                for (int i = 0; i < sampleStrokes[h].Count; i++)
                {//represents each stroke, in order


                    float strokeLength = 0;
                    for (int j = 0; j < sampleStrokes[h][i].Count; j++)
                    {   //represents each vector
                        try
                        {
                            source.Strokes[h][i][j] = weight * (source.RecordCount * source.Strokes[h][i][j] + sampleStrokes[h][i][j]);
                            strokeLength += source.Strokes[h][i][j].Magnitude();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Bad Symbol:" + sample.Name + " at stroke " + h + " precision " + Math.Pow(2, i) + " vector " + j + "\n");
                            //SpellManager.thisOne.t.text = "Bad Spell:" + s.spellName + " at stroke " + h + " precision " + Math.Pow(2, i) + " vector " + j + "\n";
                        }
                    }
                    currentLength += strokeLength;
                    strokeLengths[h].Add(strokeLength);
                }
                distances[h] += currentLength;
            }

            source.StrokeTotalLengths = distances;
            source.StrokeLengths = strokeLengths;
            source.RecordCount++;
            source.Complexity = SymbolProcessor.CalcSymbolDataEntropy(source);
            SymbolProcessor.UpdateCentroid(source);
        }
        /// <summary>
        /// The precision layer of the two symbolData sets are compared in the following ways:
        /// Spatial: How far apart is each stroke -> Tells of arrangement
        /// Segment: How do the stroke component vectors compare in direction
        /// Flow:    How does the flow (pathing) of the two symbolData strokes compare?
        /// Proportion: How do the proportional lengths of each stroke compare?
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sample"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static float LayerCompare(SymbolData source, SymbolData sample, int precision)
        {
            int hitNumSpatial = 0;
            int hitNumStroke = 0;
            int hitNumFlow = 0;
            float sourceLength = source.StrokeTotalLengths[precision];
            float sampleLength = sample.StrokeTotalLengths[precision];
            float percentDiffSpatial = 0;
            float percentDiffStroke = 0;
            float percentDiffFlow = 0;
            float percentDiffProportion = 0;
            //Need to ignore the first strokeEndVector (which is just starting point)
            if (source.StrokeEndVectors.Count > 0)
                for (int i = 1; i < sample.StrokeEndVectors.Count; i++)// gaps between strokes
                {
                    float diffEVSample = CalculateDiff(source.StrokeEndVectors[i], sample.StrokeEndVectors[i], sourceLength, sampleLength);
                    //float[] delta = Util.difference(sample.strokeEndVectors[i], source.strokeEndVectors[i]);
                    //float num = Util.magnitude(delta);
                    percentDiffSpatial = (diffEVSample + percentDiffSpatial * hitNumSpatial) / (hitNumSpatial + 1);
                    hitNumSpatial++;
                }


            var sampleStrokes = sample.Strokes[precision];

            //Calculate difference in stroke individual vector components
            for (int i = 0; i < sampleStrokes.Count; i++)
            {//represents each stroke, in order
                for (int j = 0; j < sampleStrokes[i].Count; j++)   //represents each vector
                {
                    float strokeDiff = Vector.AngleBetween(source.Strokes[precision][i][j], sampleStrokes[i][j]) / Mathf.PI;
                    percentDiffStroke = (strokeDiff + percentDiffStroke * hitNumStroke) / (hitNumStroke + 1);
                    hitNumStroke++;
                }
                //float sourceSegmentLength = source.strokeLengths[precision][i];
                //float sampleSegmentLength = sample.strokeLengths[precision][i];
                //float percentSegmentLengthTotalDiff = Mathf.Abs(sourceSegmentLength / sourceLength - sampleSegmentLength / sampleLength);
                //percentDiffProportion += percentSegmentLengthTotalDiff;
            }
            //percentDiffProportion /= source.strokeCount;

            //Calculate difference in angles between vectors
            for (int i = 0; i < sampleStrokes.Count; i++)//represents each stroke, in order
                for (int j = 1; j < sampleStrokes[i].Count; j++)   //represents each vector
                {
                    float thetaSource = Vector.AngleBetween(source.Strokes[precision][i][j], source.Strokes[precision][i][j - 1]);
                    float thetaSample = Vector.AngleBetween(sampleStrokes[i][j], sampleStrokes[i][j - 1]);
                    float thetaDiff = Mathf.Abs(thetaSource - thetaSample) / (2 * Mathf.PI); //max is 2PI (src +PI sample -PI) min is -2PI (src  -PI sample +PI)
                    percentDiffFlow = (thetaDiff + percentDiffFlow * hitNumFlow) / (hitNumFlow + 1);
                    hitNumFlow++;
                }
            //Debug.Log(sample.name + " Segment " + percentDiffSegment + " Spatial " + percentDiffSpatial + " Flow " + percentDiffFlow + " Proportion " + percentDiffProportion);
            return 1 - (percentDiffStroke + percentDiffSpatial + percentDiffFlow) / 3f;// + .25f * percentDiffProportion;
        }
        public static float FullCompare(SymbolData source, SymbolData sample)
        {
            float scaleSum = 0;
            float scaledResult = 0;
            for (int i = 0; i <= SymbolProcessor.MAXLEVEL; i++)
            {
                float scale = 1 << i;
                float compareResult = LayerCompare(source, sample, i);
                scaledResult += compareResult * scale;
                scaleSum += scale;

            }
            return scaledResult / scaleSum;
        }
        /// <summary>
        /// Used for classifying spells as they are being written based on similarity so far
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sample"></param>
        /// <param name="strokeIndex"></param>
        /// <param name="comprehensive"></param>
        /// <returns></returns>
        public static float SymbolCompare(SymbolData source, SymbolData sample, int strokeIndex, bool comprehensive)
        {
            int startIndex = comprehensive ? 0 : strokeIndex;
            float scaledSum = 0;
            float totalError = 0;

            List<Stroke> sampleStrokes;
            float sourceLength, sampleLength, percentDiffStroke, percentDiffFlow, percentDiffScale, percentDiffSpatial;
            int hitNumStroke, hitNumFlow, hitNumSpatial;
            for (int k = 0; k <= SymbolProcessor.MAXLEVEL; k++)
            {
                float errorScale = 1 << k;

                sourceLength = source.StrokeTotalLengths[k];
                sampleLength = sample.StrokeTotalLengths[k];
                percentDiffStroke = 0;//Difference in stroke angles
                percentDiffFlow = 0;//Difference in changes in stroke angles
                percentDiffScale = 0;//Scale of writing
                percentDiffSpatial = 0;//Spatial difference between strokes
                sampleStrokes = sample.Strokes[k];
                hitNumStroke = 0;
                hitNumFlow = 0;
                hitNumSpatial = 0;
                //Need to ignore the first strokeEndVector (which is just starting point). Also make sure that the start stroke is NOT the first one, because then connections cannot be counted
                if (source.StrokeEndVectors.Count > 0 && startIndex > 0 && comprehensive)
                {
                    for (int i = startIndex; i < sample.StrokeEndVectors.Count; i++)// gaps between strokes
                    {
                        //Screw scale invariance
                        float diffEVSample = CalculateDiff(source.StrokeEndVectors[i], sample.StrokeEndVectors[i], sourceLength, sampleLength);
                        percentDiffSpatial = (diffEVSample + percentDiffSpatial * hitNumSpatial) / (hitNumSpatial + 1);
                        hitNumSpatial++;
                    }
                }

                //Calculate difference in stroke individual vector components
                for (int i = startIndex; i <= strokeIndex; i++)
                {//represents each stroke, in order
                    for (int j = 0; j < sampleStrokes[i].Count; j++)   //represents each vector
                    {
                        //Note: the calculateDiff function using stroke length isn't used, because the length of each vector is the same
                        float strokeDiff = Vector.AngleBetween(source.Strokes[k][i][j], sampleStrokes[i][j]) / Mathf.PI;
                        percentDiffStroke = (strokeDiff + percentDiffStroke * hitNumStroke) / (hitNumStroke + 1);
                        hitNumStroke++;
                    }
                    /*float sourceSegmentLength = source.strokeLengths[k][i];
                    float sampleSegmentLength = sample.strokeLengths[k][i];
                    float percentSegmentLengthRawDiff = 1 - normalPDF((sourceSegmentLength - sampleSegmentLength) / sampleSegmentLength); //Mathf.Abs((sourceSegmentLength - sampleSegmentLength) / (sourceSegmentLength + sampleSegmentLength));
                    //float percentSegmentLengthTotalDiff = Mathf.Abs(sourceSegmentLength / sourceLength - sampleSegmentLength / sampleLength);
                    percentDiffScale += percentSegmentLengthRawDiff;*/
                }
                //percentDiffScale /= source.strokeCount;


                //Calculate difference in angles between vectors
                for (int i = startIndex; i <= strokeIndex; i++)//represents each stroke, in order
                    for (int j = 1; j < sampleStrokes[i].Count; j++)   //represents each vector
                    {
                        float thetaSource = Vector.AngleBetween(source.Strokes[k][i][j], source.Strokes[k][i][j - 1]);
                        float thetaSample = Vector.AngleBetween(sampleStrokes[i][j], sampleStrokes[i][j - 1]);
                        float thetaDiff = Mathf.Abs(thetaSource - thetaSample) / (2 * Mathf.PI); //max is 2PI (src +PI sample -PI) min is -2PI (src  -PI sample +PI)
                        percentDiffFlow = (thetaDiff + percentDiffFlow * hitNumFlow) / (hitNumFlow + 1);
                        hitNumFlow++;
                    }
                //Just not using scale because it doesn't seem to help much
                totalError += errorScale * (percentDiffStroke + percentDiffFlow + percentDiffSpatial) / 3;
                scaledSum += errorScale;
            }

            return 1 - totalError / scaledSum;
        }
        public static float NormalPDF(float z)
        {
            return 1f / Mathf.Sqrt(2 * Mathf.PI) * Mathf.Exp(-.5f * z * z);
        }
        //Screw scale invariance
        public static float CalculateDiff(Vector v1, Vector v2, float v1StrokeLength, float v2StrokeLength)
        {
            float mag1 = v1.Magnitude();
            float mag2 = v2.Magnitude();
            float percentDirDiff = Vector.AngleBetween(v1, v2) / Mathf.PI; //Always 0->1
            float percentLenDiff = /*Mathf.Abs((mag1 - mag2) / (mag1 + mag2));//*/ Mathf.Abs(mag1 / v1StrokeLength - mag2 / v2StrokeLength);
            return (percentLenDiff * .3f + percentDirDiff * .7f);
        }
    }
}