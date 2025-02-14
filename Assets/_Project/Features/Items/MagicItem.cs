using BacklineVR.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// Magic Items return to inventory when released, and require casting to use. They have a fixed number of charges that reload over time, 
    /// and using the last charge will make it more powerful but also sacrifice it.
    /// These facilitate casting magic on the spell with the targeting system's selection
    /// </summary>
    public class MagicItem : Item
    {
        [SerializeField]
        private string _magicItemCode;
        [SerializeField]
        private int _totalCharges;
        private int _chargesRemaining;
        private float usageCooldown;
        private TargetingSystem _targetingSystem;
        private void Awake()
        {
            _targetingSystem = GetComponent<TargetingSystem>();
        }
        public override void Activate()
        {
            print("Weapon armed!");
            //Arm this item, tell inputListener to swap to magic input listener. Magic item is now armed
            Player.Instance.SetInputMode(Interaction.InputMode.MagicCasting);
            _targetingSystem.Activate(Owner.OtherHand.transform);
        }
        public override void Deactivate()
        {
            print("Weapon disarmed!");
            Player.Instance.SetInputMode(Interaction.InputMode.Default);
            _targetingSystem.Deactivate();
        }
        public void StartSelection()
        {
            _targetingSystem.StartSelect();
        }
        public void StopSelection()
        {
            _targetingSystem.StopSelect();
        }
        public void Cast()
        {
            _targetingSystem.ApplySelections();
        }
        public override string GetCode() => _magicItemCode;
    }
}