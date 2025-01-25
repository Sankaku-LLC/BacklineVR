using BacklineVR.Characters;
using BacklineVR.Interaction;
using BacklineVR.Interaction.Bow;
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

        [SerializeField]
        private Interactable_Bad _longBow;

        [SerializeField]
        private Interactable_Bad _arrowHand;

        [SerializeField]
        private Hand _leftHand;

        [SerializeField]
        private Hand _rightHand;
        private bool _isKilled;
        private bool _isStaggered;
        public bool IsDestroyed() => _isKilled;
        public bool IsStaggered() => _isStaggered;

        public GameObject GetGameObject() => this.gameObject;
        public Transform GetMainTransform() => this.transform;
        public virtual TargetType GetTargetType() => TargetType.Player;

        private InputProvider _inputProvider;
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
            //_leftHand.AttachObject(_longBow.gameObject, GrabTypes.Scripted);
            //_rightHand.AttachObject(_arrowHand.gameObject, GrabTypes.Scripted);
        }

        // Update is called once per frame
        void Update()
        {

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
        public Transform GetArrowNockTransform()
        {
            if (IsLeftHanded)
            {
                return _leftHand.ArrowNockTransform;
            }
            return _rightHand.ArrowNockTransform;
        }
    }
}