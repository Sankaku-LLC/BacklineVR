using BacklineVR.Casting;
using BacklineVR.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Interaction
{
    public class DefaultInputListener : InputListener
    {
        private readonly Dictionary<HandSide, Hand> _hands = new Dictionary<HandSide, Hand>(2);
        private readonly Dictionary<HandSide, Item> _possessions = new Dictionary<HandSide, Item>(2);
        private readonly Dictionary<HandSide, GrabCaster> _grabCasters = new Dictionary<HandSide, GrabCaster>(2);

        private HandSide Dominant;
        private HandSide NonDominant;

        [SerializeField]
        private Hand _leftHand;
        [SerializeField]
        private Hand _rightHand;

        public override InputMode GetInputMode() => InputMode.Default;
        private protected override void Initialize()
        {
            _hands[HandSide.Left] = _leftHand;
            _hands[HandSide.Right] = _rightHand;
            _grabCasters[HandSide.Left] = _leftHand.GetComponent<GrabCaster>();
            _grabCasters[HandSide.Right] = _rightHand.GetComponent<GrabCaster>();
        }
        private protected override void Setup()
        {
            Dominant = _player.IsLeftHanded ? HandSide.Left : HandSide.Right;
            NonDominant = _player.IsLeftHanded ? HandSide.Right : HandSide.Left;
        }
        public override void Subscribe(InputProvider provider)
        {
            provider.OnGripDown += OnGripDown;
            provider.OnGripUp += OnGripUp;
            provider.OnTriggerDown += OnTriggerDown;
            provider.OnTriggerUp += OnTriggerUp;
        }

        public override void Unsubscribe(InputProvider provider)
        {
            provider.OnGripDown -= OnGripDown;
            provider.OnGripUp -= OnGripUp;
            provider.OnTriggerDown -= OnTriggerDown;
            provider.OnTriggerUp -= OnTriggerUp;
        }

        private protected override void OnGripDown(HandSide side, float amount)
        {
            //Check if this hand was attempting a grab cast. If so, try to find what item it was
            //If it was a valid item, give that item to that player
            //Else do a grab in space
            var interactable = _hands[side].GrabHovered();
            _possessions[side] = interactable.GetComponent<Item>();
        }
        private protected override void OnGripUp(HandSide side)
        {
            _hands[side].DetachObject(true);//Later set it to false if we are throwing a throwable
            _possessions[side] = null;
        }
        private protected override void OnTriggerDown(HandSide side, float amount)
        {
            if (_possessions[side] != null)
            {
                _possessions[side].Activate();
                return;
            }

            //Begin casting 
        }
        private protected override void OnTriggerUp(HandSide side)
        {
            if (side == HandSide.Left)
            {
                return;
            }
        }
    }
}