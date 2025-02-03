using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// Magic Items return to inventory when released, and require casting to use. They have a fixed number of charges that reload over time, 
    /// and using the last charge will make it more powerful but also sacrifice it.
    /// </summary>
    [RequireComponent(typeof(Spell))]
    public class MagicItem : Item
    {
        [SerializeField]
        private string _magicItemCode;
        private Spell _spell;
        private int _chargesRemaining;
        private float usageCooldown;
        public override void Activate()
        {
        }
        public override void Deactivate()
        {
        }

        public override string GetCode() => _magicItemCode;
    }
}