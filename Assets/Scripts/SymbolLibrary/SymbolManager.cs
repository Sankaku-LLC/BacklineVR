using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class uses operators from the SymbolManager and SymbolProcessor class to do meta-operations, such as classification. Contains the actual data
/// </summary>

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
namespace CurseVR.SymbolSystem
{
    /// <summary>
    /// Symbol Manager can manage multiple pools of segments
    /// </summary>
    public class SymbolManager
    {
        private const float THRESHOLD = 0.5f;
        private string[] _directories;
        private List<Dictionary<int, List<SymbolData>>> _symbolDataTable;// SegmentData entries keyed by stroke count
        private List<Dictionary<string, SymbolData>> _symbolDataNametable;// SegmentData entries keyed by name
        private int[] _maxStrokeCount;
        //If this is true, load from StreamingAssets folder instead, and all write operations are invalidated
        private bool[] _readOnlySymbol;


        public SymbolManager(string[] directory, bool[] readOnlySymbol)
        {
            _directories = directory;
            _readOnlySymbol = readOnlySymbol;

            _symbolDataTable = new List<Dictionary<int, List<SymbolData>>>(_directories.Length);
            _symbolDataNametable = new List<Dictionary<string, SymbolData>>(_directories.Length);
            _maxStrokeCount = new int[_directories.Length];
            for (int i = 0; i < _directories.Length; i++)
            {
                _symbolDataTable.Add(new Dictionary<int, List<SymbolData>>());
                _symbolDataNametable.Add(new Dictionary<string, SymbolData>());
            }
            for (int i = 0; i < _directories.Length; i++)
            {

                //Check if the player's personal segment system directory exists yet
                if (!readOnlySymbol[i])
                {
                    //If the user data path does not exist, copy from 
                    if (!Directory.Exists(Application.persistentDataPath + "/" + directory[i]))
                    {
                        Directory.CreateDirectory(Application.persistentDataPath + "/" + directory[i]);
                        //Transfer files
                        LoadDefaultSymbols(i, directory[i]);
                    }
                }
            }

        }
        public List<SymbolData> GetSymbolsInPool(int pool)
        {
            if (pool >= _symbolDataNametable.Count)
            {
                Debug.LogError("Pool number is out of bounds!");
                return null;
            }
            else
            {
                var symbolEntries = _symbolDataNametable[pool];
                var symbolsInPool = new List<SymbolData>();
                foreach (var entry in symbolEntries)
                {
                    symbolsInPool.Add(entry.Value);
                }
                return symbolsInPool;
            }
        }
        public SymbolData GetSymbolByName(int pool, string name)
        {
            if (_symbolDataNametable[pool].ContainsKey(name))
            {
                return _symbolDataNametable[pool][name];
            }
            else
            {
                return null;
            }
        }
        public void SaveSymbolData(int pool, SymbolData symbol)
        {
            if (!_readOnlySymbol[pool] && !string.IsNullOrEmpty(symbol.Name) && symbol.StrokeCount > 0)
            {
                FileStream file2;
                file2 = File.Create(Application.persistentDataPath + "/" + _directories[pool] + "/" + symbol.Name + ".dat");
                BinaryFormatter bf2 = new BinaryFormatter();
                bf2.Serialize(file2, symbol);
                file2.Close();
                AddSymbolDataEntry(pool, symbol);
            }
            else
            {
                Debug.Log("Name is empty or strokes are zero: " + symbol.Name + " " + symbol.StrokeCount + " " + pool);
            }
        }
        public void RemoveSymbolData(int pool, string name)
        {
            var s = GetSymbolByName(pool, name);
            if (s == null)
            {
                Debug.LogError("No symbol by name of " + name + " exists in pool " + pool);
                return;
            }
            if (!_readOnlySymbol[pool] && s.Name != "" && s.StrokeCount > 0)
            {
                File.Delete(Application.persistentDataPath + "/" + _directories[pool] + "/" + s.Name + ".dat");
                _symbolDataTable[pool][s.StrokeCount].Remove(s);
                _symbolDataNametable[pool].Remove(s.Name);
            }
        }
        public SymbolData LoadSymbolData(int pool, string s)
        {
            if (File.Exists(s))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(s, FileMode.Open);
                SymbolData data = (SymbolData)bf.Deserialize(file);
                file.Close();
                //Fix the data entry, and save it and read the correct complexity
                if (data.Complexity == -1)
                {
                    Debug.Log("Negative detected " + s);
                    data.Complexity = SymbolProcessor.CalcSymbolDataEntropy(data);
                    SaveSymbolData(pool, data);
                }
                return data;
            }
            else
            {
                Debug.LogError("File does not exist");
                return null;
            }

        }
        public void TrainSymbol(int pool, SymbolData source, SymbolData sample)
        {
            SymbolOperator.Merge(source, sample);
            SaveSymbolData(pool, source);
        }
        public List<SymbolData> GetNearbyStartSymbols(int pool, Vector3 cameraSpacePoint)
        {
            var cameraSpaceVector = new Vector(new float[] { cameraSpacePoint.x, cameraSpacePoint.y, cameraSpacePoint.z });
            List<SymbolData> output = new List<SymbolData>();
            foreach (string s in _symbolDataNametable[pool].Keys)
            {
                SymbolData data = _symbolDataNametable[pool][s];
                output.Add(data);
            }
            output.Sort((a, b) =>
            {
                float dA = Vector.Distance(a.StrokeEndVectors[0], cameraSpaceVector);
                float dB = Vector.Distance(b.StrokeEndVectors[0], cameraSpaceVector);
                return dA > dB ? 1 : -1;
            }
            );
            return output;
        }
        public List<ClassificationResult> PartialClassify(int pool, SymbolData source, int strokeCount, bool comprehensive = false, List<ClassificationResult> candidates = null)
        {
            List<SymbolData> searchSpace = new List<SymbolData>();
            if (candidates != null)
            {
                foreach (ClassificationResult candidate in candidates)
                {
                    if (_symbolDataNametable[pool][candidate.MatchName].StrokeCount > strokeCount)//To predict next step, length must be longer than current (don't try and predict segs 1 for 1 stroke)
                        searchSpace.Add(_symbolDataNametable[pool][candidate.MatchName]);
                }
            }
            else
            {
                for (int i = source.StrokeCount; i <= _maxStrokeCount[pool]; i++)
                {
                    if (_symbolDataTable[pool].ContainsKey(i))
                    {
                        searchSpace.AddRange(_symbolDataTable[pool][i]);
                    }
                }
            }
            //need to duplicate or else dictionary reference values will disappear!
            List<ClassificationResult> classifications = new List<ClassificationResult>();
            for (int i = 0; i < searchSpace.Count; i++)
            {
                var matchPercent = SymbolOperator.SymbolCompare(source, searchSpace[i], strokeCount - 1, comprehensive);
                var complexity = searchSpace[i].Complexity;
                var matchName = searchSpace[i].Name;
                var customData = searchSpace[i].CustomData;
                ClassificationResult result = new ClassificationResult();
                result.MatchName = matchName;
                result.SourceComplexity = complexity;
                result.MatchPercent = matchPercent;
                result.CustomData = customData;
                classifications.Add(result);
            }
            classifications.Sort((a, b) => a.MatchPercent < b.MatchPercent ? 1 : -1);
            return classifications;
        }
        public ClassificationResult ClassifyStroke(int pool, SymbolData source)
        {
            ClassificationResult result = new ClassificationResult();
            //Debug.Log("Initial info: " + segmentDataTable[pool].Keys.Count + " " + source.segmentCount);

            if (source == null || !_symbolDataTable[pool].ContainsKey(source.StrokeCount))
            {
                Debug.Log("No existing entry with that stroke count found");
                result.MatchPercent = -1;
                return result;
            }
            else
            {
                //need to duplicate or else dictionary reference values will disappear!
                List<SymbolData> potentialMatches = new List<SymbolData>(_symbolDataTable[pool][source.StrokeCount]);
                List<float> matchScores = new List<float>();
                for (int i = 0; i < potentialMatches.Count; i++)
                {
                    matchScores.Add(0);
                }
                //Probably more efficient way of doing this overall, but given I'm iterating over it and I'm adding anyways, shouldn't impact performance too much
                float scaledSum = 0;
                for (int j = 0; j <= SymbolProcessor.MAXLEVEL; j++)
                {
                    float errorScale = 1 << j;

                    //Debug.Log("______Degree: " + j + " _____");
                    for (int i = potentialMatches.Count - 1; i >= 0; i--)
                    {

                        SymbolData sd = potentialMatches[i];
                        float closeness = SymbolOperator.LayerCompare(source, sd, j);
                        //difference hard threshold based filtering
                        if (closeness < THRESHOLD)//If less than 50% close remove it from options
                        {
                            int idx = potentialMatches.IndexOf(sd);
                            potentialMatches.RemoveAt(idx);
                            matchScores.RemoveAt(idx);

                            if (potentialMatches.Count == 1) //process of elimination reesult
                            {
                                result.MatchName = potentialMatches[0].Name;
                                float matchPercent = SymbolOperator.FullCompare(source, potentialMatches[0]);
                                result.MatchPercent = matchPercent;
                                result.SourceComplexity = potentialMatches[0].Complexity;
                                result.CustomData = potentialMatches[0].CustomData;
                                //Debug.Log("Process of elimination: " + segmentDataTable.Keys.Count + " " + segmentDataTable[1].Count);

                                return result;
                            }
                        }
                        else
                        {
                            matchScores[i] += closeness * errorScale;//scales error up by the number of samples there were
                        }

                    }
                    scaledSum += errorScale;
                }
                if (potentialMatches.Count == 0)
                {
                    result.MatchPercent = -1;
                    return result;
                }
                else
                {
                    //Note: Not dividing, sum of closeness is enough (scaling doesn't change who is bigger if factor is uniform)
                    int closestIndex = -1;
                    float closestScore = 0;

                    for (int i = 0; i < matchScores.Count; i++)
                    {
                        if (matchScores[i] > closestScore)
                        {
                            closestScore = matchScores[i];
                            closestIndex = i;
                        }
                    }
                    SymbolData closest = potentialMatches[closestIndex];

                    result.MatchName = closest.Name;
                    //Need to re-normalize it
                    result.MatchPercent = closestScore / scaledSum;
                    result.SourceComplexity = closest.Complexity;
                    result.CustomData = closest.CustomData;
                    return result;
                }
            }
        }


        public void LoadDefaultSymbols(int pool, string dir)
        {
            BinaryFormatter bf = new BinaryFormatter();
            var assets = Resources.LoadAll<TextAsset>(dir);
            foreach (var asset in assets)
            {
                var stream = new MemoryStream(asset.bytes);
                var symbolData = (SymbolData)bf.Deserialize(stream);
                SaveSymbolData(pool, symbolData);
            }
        }

        public void LoadAllSymbolData(int pool)
        {
            var fileList = Directory.GetFiles(Application.persistentDataPath + "/" + _directories[pool] + "/");
            foreach (string filePath in fileList)
            {
                SymbolData symbolData = LoadSymbolData(pool, filePath);
                AddSymbolDataEntry(pool, symbolData);
                //Debug.Log(segmentData.name + " " + segmentData.segmentCount);
            }
            //Debug.Log("Done loading segment data into table");
        }
        public void AddSymbolDataEntry(int pool, SymbolData symbolData)
        {
            int strokeCount = symbolData.StrokeCount;
            if (!_symbolDataTable[pool].ContainsKey(strokeCount))
            {
                _symbolDataTable[pool].Add(strokeCount, new List<SymbolData>());
                if (strokeCount > _maxStrokeCount[pool])
                {
                    _maxStrokeCount[pool] = strokeCount;
                }
            }

            if (!_symbolDataNametable[pool].ContainsKey(symbolData.Name))
            {
                _symbolDataNametable[pool].Add(symbolData.Name, symbolData);
                _symbolDataTable[pool][strokeCount].Add(symbolData);
            }
            else
            {
                var oldSymbolData = _symbolDataNametable[pool][symbolData.Name];
                var oldSymbolIdx = _symbolDataTable[pool][strokeCount].IndexOf(oldSymbolData);
                _symbolDataNametable[pool][symbolData.Name] = symbolData;
                _symbolDataTable[pool][strokeCount][oldSymbolIdx] = symbolData;
            }
        }

    }

}