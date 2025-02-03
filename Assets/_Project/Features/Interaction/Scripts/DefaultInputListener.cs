using BacklineVR.Casting;
using BacklineVR.Core;
using BacklineVR.Items;
using CurseVR.Director;
using CurseVR.SymbolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Interaction
{
    public class DefaultInputListener : InputListener
    {
        private readonly Dictionary<HandSide, Hand> _hands = new Dictionary<HandSide, Hand>(2);
        private readonly Dictionary<HandSide, Holdable> _possessions = new Dictionary<HandSide, Holdable>(2);
        private readonly Dictionary<HandSide, GrabCaster> _grabCasters = new Dictionary<HandSide, GrabCaster>(2);

        private SymbolApplication _symbolApp;
        private HandSide Dominant;
        private HandSide NonDominant;


        [SerializeField]
        private Hand _leftHand;
        [SerializeField]
        private Hand _rightHand;

        public override InputMode GetInputMode() => InputMode.Default;
        private protected override void Initialize()
        {
            _symbolApp = GlobalDirector.Get<SymbolApplication>();

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
            if (_possessions[side] != null)
            {
                Debug.LogError("Grip down called while holding an item shouldn't be possible!");
                return;
            }

            //Check if this hand was attempting a grab cast. If so, try to find what item it was
            //If it was a valid item, give that item to that player
            var successfulCast = _grabCasters[side].TryCastSymbol(out var symbolData);
            if (successfulCast)
            {
                var didSpawn = TrySpawnItem(symbolData, out var spawnedItem);
                if (didSpawn)
                {
                    _possessions[side] = spawnedItem;
                    _hands[side].HoldItem(spawnedItem);
                    return;
                }
            }

            //Else do a grab in space
            var holdable = _hands[side].GrabHovered();
            if (holdable == null)
                return;
            _possessions[side] = holdable;
        }
        private bool TrySpawnItem(SymbolData symbolData, out Item item)
        {
            //_symbolApp.Save(SymbolPool.Curse, _spellNames[0], cast);
            //_spellNames.RemoveAt(0);
            var successfulClassification = _symbolApp.TryClassify(SymbolPool.Curse, symbolData, out var result);
            if (!successfulClassification)
            {
                //    return;
            }
            item = null;
            return false;
        }
        private protected override void OnGripUp(HandSide side)
        {
            _hands[side].DetachObject(true);//Later set it to false if we are throwing a throwable
            _possessions[side] = null;
        }
        private protected override void OnTriggerDown(HandSide side, float amount)
        {
            if (_possessions[side] is Item item)
            {
                item.Activate();
                return;
            }

            //Begin casting 
            _grabCasters[side].StartStroke();
        }
        private protected override void OnTriggerUp(HandSide side)
        {
            if (_possessions[side] is Item item)
            {
                item.Deactivate();
                return;
            }
            var wasSuccessful = _grabCasters[side].TryEndStroke();//Use this to trigger preview
        }
    }
}