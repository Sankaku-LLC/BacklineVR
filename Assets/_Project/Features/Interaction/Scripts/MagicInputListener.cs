using BacklineVR.Casting;
using BacklineVR.Items;
using CurseVR.Director;
using CurseVR.SymbolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BacklineVR.Interaction
{
    //We enter this state upon a magic item being held and the trigger is pressed (non dominant hand)
    /// <summary>
    /// Listen for dominant trigger down for spell targeting changes
    /// Listen for dominant grip down for spell activation 
    /// </summary>
    public class MagicInputListener : InputListener
    {
        private MagicItem _magicItem;
        public override InputMode GetInputMode() => InputMode.MagicCasting;
        public override void Subscribe(InputProvider provider)
        {
            _magicItem = (MagicItem)_player.GetPossession(NonDominant);
            if (_player.GetPossession(Dominant) != null)
                _player.ReleaseItem(Dominant);//Auto-stash throwable item
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
            _magicItem = null;
        }
        private protected override void OnGripDown(HandSide side, float amount)
        {
            if (side == NonDominant)
            {
                Debug.LogError("Grip down should not be registerable in magic input mode for nondominant");
                return;
            }
            _magicItem.Cast();
        }

        private protected override void OnGripUp(HandSide side)
        {
            if (side == Dominant)
                return;
            _player.ReleaseItem(NonDominant);//Let item call SetInputMode and handle resetting in unsubscribe
        }
        private protected override void OnTriggerDown(HandSide side, float amount)
        {
            if (side == NonDominant)
            {
                return;
            }
            _magicItem.StartSelection();
        }
        private protected override void OnTriggerUp(HandSide side)
        {
            if (side == NonDominant)
            {
                _magicItem.Deactivate();
                return;
            }
            _magicItem.StopSelection();
        }
    }
}