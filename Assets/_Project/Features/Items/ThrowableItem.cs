using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// These may have countdowns for a fuse, or activate on hit (depends). 
    /// They can be thrown when released, and as such won't return directly to the inventory upon released unlike magic items
    /// They are destroyed after collision
    /// </summary>
    public class ThrowableItem : Item
    {
        [SerializeField]
        private string _throwableItemCode;
        private float _countDown;
        public override void Activate()
        {
        }

        public override void Deactivate()
        {
        }

        public override string GetCode() => _throwableItemCode;
    }
}