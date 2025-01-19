using System;
using System.Collections;
using System.Collections.Generic;
using CurseVR.Director;
using CurseVR.SymbolSystem;
using UnityEngine;
namespace BacklineVR.Casting
{
    public enum SymbolPool { None = -1, Control = 0, Curse = 1 }
    /// <summary>
    /// Is the top, user level layer for symbol manipulations
    /// </summary>
    public class SymbolApplication : MonoBehaviour, IGlobalComponent
    {
        /// <summary>
        /// Edit this variable with each update of the symbol system to replace default symbol added to player's corpus (and in the future trigger symbol version updating)
        /// </summary>
        const string SYMBOLSYSTEM_VER = "0.0.0";
        private readonly bool[] _directoryIsReadOnly = new bool[] { true, false };
        private readonly string[] _rootDirectory = new string[] { "Controls", "Curses" };

        private SymbolManager _symbolManager;

        public enum AppMode { View = 0, Merge = 1, Save = 2, Classify = 3 }


        public void OnInitialize()
        {
            //this used to reference "Symbols" alone. Should reference "Symbols" then "Controls"
            _symbolManager = new SymbolManager(_rootDirectory, _directoryIsReadOnly);

            //Load control symbols
            _symbolManager.LoadDefaultSymbols((int)SymbolPool.Control, _rootDirectory[(int)SymbolPool.Control]);

            //This will copy symbols from streamingAssets (developer-generated, read-only) to persistentDatapath (user symbol space)
            //This runs if the spell system version was updated and the old spells need to be replaced on builds
            //if (PlayerPrefs.GetString("SYMBOLSYSTEM_VER") != SYMBOLSYSTEM_VER)
            {
                _symbolManager.LoadDefaultSymbols((int)SymbolPool.Curse, _rootDirectory[(int)SymbolPool.Curse]);
                PlayerPrefs.SetString("SYMBOLSYSTEM_VER", SYMBOLSYSTEM_VER);
            }

            for (int i = 0; i < _directoryIsReadOnly.Length; i++)
            {
                if (!_directoryIsReadOnly[i])
                    _symbolManager.LoadAllSymbolData(i);
            }
        }
        public void OnStart()
        {

        }
        public Type GetManagerType() => typeof(SymbolApplication);


        public List<SymbolData> GetSymbolsInPool(SymbolPool pool)
        {
            return _symbolManager.GetSymbolsInPool((int)pool);
        }
        public SymbolData GetSymbol(SymbolPool pool, string name)
        {
            return _symbolManager.GetSymbolByName((int)pool, name);
        }
        public List<ClassificationResult> PartialMatchSigils(SymbolPool pool, SymbolData currentPartialData, bool comprehensive = false, List<ClassificationResult> candidates = null)
        {
            return _symbolManager.PartialClassify((int)pool, currentPartialData, currentPartialData.StrokeCount, comprehensive, candidates);
        }
        public List<SymbolData> GetNearbyStartSymbols(SymbolPool pool, Vector3 position)
        {
            return _symbolManager.GetNearbyStartSymbols((int)pool, position);
        }
        public SymbolData GetSymbolByName(SymbolPool pool, string targetName)
        {
            return _symbolManager.GetSymbolByName((int)pool, targetName);//doing this for the same reason as others
        }
        /// <summary>
        /// data takes on the merged information for processing by specialized implementations
        /// </summary>
        /// <param name="data"></param>
        public SymbolData Merge(SymbolPool pool, string targetName, SymbolData data)
        {
            SymbolData sd = _symbolManager.GetSymbolByName((int)pool, targetName);
            if (sd != null)
            {
                SymbolOperator.Merge(sd, data);
            }
            return sd;
        }
        public void Delete(SymbolPool pool, string targetName)
        {
            _symbolManager.RemoveSymbolData((int)pool, targetName);
        }
        public void Save(SymbolPool pool, string targetName, SymbolData data)
        {
            data.Name = targetName;
            _symbolManager.SaveSymbolData((int)pool, data);
        }
  
        public bool TryClassify(SymbolPool pool, SymbolData data, out ClassificationResult result)
        {
            result = default;
            if (data == null)
                return false;
            result = _symbolManager.ClassifyStroke((int)pool, data);
            if (result.MatchPercent > 0)
            {
                return true;
            }
            return false;
        }
    }
}