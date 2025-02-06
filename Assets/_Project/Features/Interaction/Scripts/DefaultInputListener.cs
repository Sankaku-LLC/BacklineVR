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

        private readonly Dictionary<HandSide, GrabCaster> _grabCasters = new Dictionary<HandSide, GrabCaster>(2);

        private SymbolApplication _symbolApp;



        public override InputMode GetInputMode() => InputMode.Default;
        private protected override void Initialize()
        {
            _symbolApp = GlobalDirector.Get<SymbolApplication>();
        }
        private protected override void Setup()
        {
            _grabCasters[HandSide.Left] = _player.GetHand(HandSide.Left).GetComponent<GrabCaster>();
            _grabCasters[HandSide.Right] = _player.GetHand(HandSide.Right).GetComponent<GrabCaster>();
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
            _grabCasters[HandSide.Left].ClearSingleStrokeData();
            _grabCasters[HandSide.Right].ClearSingleStrokeData();
            provider.OnGripDown -= OnGripDown;
            provider.OnGripUp -= OnGripUp;
            provider.OnTriggerDown -= OnTriggerDown;
            provider.OnTriggerUp -= OnTriggerUp;
        }

        private protected override void OnGripDown(HandSide side, float amount)
        {
            if (_player.GetPossession(side) != null)
            {
                Debug.LogError("Grip down called while holding an item shouldn't be possible!");
                return;
            }

            //Check if this hand was attempting a grab cast. If so, try to find what item it was
            //If it was a valid item, give that item to that player
            var successfulCast = _grabCasters[side].TryCastSymbol(out var symbolData);
            if (successfulCast)
            {
                var itemFound = TryFindItem(side, symbolData, out var itemCode);
                if (itemFound)
                {
                    _player.GiveItem(side, itemCode);
                    return;
                }
            }

            //Else do a grab in space
            _player.GrabNear(side);

            //Else open the grab menu, and on selection give them the item.
        }
        private bool TryFindItem(HandSide side, SymbolData symbolData, out string itemCode)
        {
            //_symbolApp.Save(SymbolPool.Curse, _spellNames[0], cast);
            //_spellNames.RemoveAt(0);
            itemCode = side == Dominant ? "ThrowableItemDummy" : "MagicItemDummy";
            return true;

            var pool = side == Dominant ? SymbolPool.ThrowableItem : SymbolPool.MagicItem;
            var successfulClassification = _symbolApp.TryClassify(pool, symbolData, out var result);
            if (!successfulClassification)
            {
                return false;
            }
            itemCode = symbolData.Name;
            return true;
        }
        private protected override void OnGripUp(HandSide side)
        {
            if (_player.GetPossession(side) == null)
                return;
            _player.ReleaseItem(side);
        }
        private protected override void OnTriggerDown(HandSide side, float amount)
        {
            if (_player.GetPossession(side) is Item item)
            {
                item.Activate();
                return;
            }

            //Begin casting 
            _grabCasters[side].StartStroke();
        }
        private protected override void OnTriggerUp(HandSide side)
        {
            if (_player.GetPossession(side) is Item item)
            {
                item.Deactivate();
                return;
            }
            var wasSuccessful = _grabCasters[side].TryEndStroke();//Use this to trigger preview
        }
    }
}