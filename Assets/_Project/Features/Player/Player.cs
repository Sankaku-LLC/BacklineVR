using BacklineVR.Casting;
using BacklineVR.Characters;
using BacklineVR.Interaction;
using BacklineVR.Items;
using CurseVR.Director;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Core
{
    public class Player : MonoBehaviour, ITargetable, IDestructible
    {
        public readonly Dictionary<HandSide, Hand> _hands = new Dictionary<HandSide, Hand>(2);
        public readonly Dictionary<HandSide, Holdable> _possessions = new Dictionary<HandSide, Holdable>(2);

        public bool IsLeftHanded = false;
        private CombatManager _combatManager;
        private ItemManager _itemManager;

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

        [SerializeField]
        private Hand _leftHand;
        [SerializeField]
        private Hand _rightHand;

        private void Awake()
        {
            Instance = this;
            _hands[HandSide.Left] = _leftHand;
            _hands[HandSide.Right] = _rightHand;
            _possessions[HandSide.Left] = null;
            _possessions[HandSide.Right] = null;
        }
        // Start is called before the first frame update
        void Start()
        {
            _itemManager = GlobalDirector.Get<ItemManager>();
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
        public Hand GetHand(HandSide side)
        {
            return _hands[side];
        }
        public Holdable GetPossession(HandSide side)
        {
            return _possessions[side];
        }
        public void GiveItem(HandSide side, string itemCode)
        {
            var gotItem = _itemManager.TryGetItem(itemCode, out var item);
            if (!gotItem)
            {
                Debug.LogError("Item was not available or not found " + itemCode);
                return;
            }
            GrabItem(side, item);
        }
        public void GrabItem(HandSide side, Holdable possession)
        {
            _possessions[side] = possession;
            _hands[side].HoldItem(possession);
        }
        public void GrabNear(HandSide side)
        {
            var holdable = _hands[side].GrabHovered();
            if (holdable == null)
                return;
            _possessions[side] = holdable;
        }
        public void ReleaseItem(HandSide side)
        {
            var holdable = _possessions[side];
            var shouldReparent = holdable.ShouldReparent();
            _hands[side].DetachObject(shouldReparent);//Later set it to false if we are throwing a throwable
            if (shouldReparent && holdable is Item item)
            {//Put back in inventory
                _itemManager.StoreItem(item);
            }
            _possessions[side] = null;
        }
    }
}