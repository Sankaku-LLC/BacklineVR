using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CurseVR.Director
{
    /// <summary>
    /// Use this to get singleton instances, initialize them, and control large-scale flow of game logic
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GlobalDirector : MonoBehaviour
    {
        private readonly static Dictionary<Type, IGlobalComponent> _cachedGlobalComponents = new Dictionary<Type, IGlobalComponent>(12);
        private void Awake()
        {
            InitializeAllManagers();
        }
        private void Start()
        {
            StartAllManagers();
        }
        private void OnDestroy()
        {
            _cachedGlobalComponents.Clear();
        }
        private void InitializeAllManagers()
        {
            var managers = GetComponentsInChildren<IGlobalComponent>(true);
            foreach (var manager in managers)
            {
                if (!_cachedGlobalComponents.ContainsKey(manager.GetManagerType()))
                {
                    _cachedGlobalComponents.Add(manager.GetManagerType(), manager);
                }
            }
            print("Initialized " + _cachedGlobalComponents.Count + " managers");
            foreach (var manager in managers)
            {
                manager.OnInitialize();
            }
        }
        private void StartAllManagers()
        {
            foreach (var manager in _cachedGlobalComponents.Values)
            {
                manager.OnStart();
            }
        }
        public static T Get<T>() where T : IGlobalComponent
        {
            if (_cachedGlobalComponents.TryGetValue(typeof(T), out var globalInterface))
            {
                return (T)globalInterface;
            }
            throw new NullReferenceException("GlobalDirector does not contain component with type of: " + typeof(T).Name);
        }
        public static bool TryGet<T>(out T globalManager) where T : IGlobalComponent
        {
            try
            {
                globalManager = Get<T>();
            }
            catch (Exception)
            {
                globalManager = default(T);
                return false;
            }
            return globalManager != null;
        }
    }

    public interface IGlobalComponent
    {
        /// <summary>
        /// Use this to initialize bindings
        /// </summary>
        void OnInitialize();
        /// <summary>
        /// Use this to execute stuff that other bindings may listen to
        /// </summary>
        void OnStart();
        Type GetManagerType();
    }
}