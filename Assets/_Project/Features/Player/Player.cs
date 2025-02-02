using BacklineVR.Casting;
using BacklineVR.Characters;
using BacklineVR.Interaction;
using CurseVR.Director;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Core
{
    public class Player : MonoBehaviour, ITargetable, IDestructible
    {
        public bool IsLeftHanded = false;
        private CombatManager _combatManager;
        public static Player Instance;
        public Transform Head;
        public Transform Origin;



        private bool _isKilled;
        private bool _isStaggered;
        public bool IsDestroyed() => _isKilled;
        public bool IsStaggered() => _isStaggered;

        public GameObject GetGameObject() => this.gameObject;
        public Transform GetMainTransform() => this.transform;
        public virtual TargetType GetTargetType() => TargetType.Player;

        private InputProvider _inputProvider;

        private readonly Dictionary<InputMode, InputListener> _inputListeners = new Dictionary<InputMode, InputListener>(8);

        private InputListener _currentMode;

        private void Awake()
        {
            Instance = this;
        }
        // Start is called before the first frame update
        void Start()
        {
            _inputProvider = GlobalDirector.Get<InputProvider>();
            _combatManager = GlobalDirector.Get<CombatManager>();
            _combatManager.OnAllySpawned(this);

            var listeners = GetComponents<InputListener>();
            foreach(var listener in listeners)
                _inputListeners.Add(listener.GetInputMode(), listener);

            SetInputMode(InputMode.Default);
        }
    
        public void SetInputMode(InputMode inputMode)
        {
            if(_currentMode != null)
            {
                _currentMode.Unsubscribe(_inputProvider);
            }
            _currentMode = _inputListeners[inputMode];
            _currentMode.Subscribe(_inputProvider);

        }
        public void TakeDamage(float damageAmount)
        {
        }
        public void TriggerHaptics(bool useDominant, float microSecondsDuration)
        {
            float seconds = (float)microSecondsDuration / 1000000f;
            //If use dominant and left handed, second case used since XOR
            var handSide = useDominant ^ IsLeftHanded ? HandSide.Right : HandSide.Left;
            _inputProvider.RequestHapticPulse(handSide, 1, seconds, 1.5f / seconds);
        }
    }
}