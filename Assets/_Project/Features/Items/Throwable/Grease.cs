using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// This is logic for the grease bottle. After being thrown 
    /// </summary>
    public class Grease : ThrowableItem
    {
        [SerializeField]
        private GameObject _bottle;
        [SerializeField]
        private GameObject _puddle;

        private bool _broken = false;

        private protected override void OnItemThrown()
        {
            throw new System.NotImplementedException();
        }

        private protected override void OnCollision()
        {
            throw new System.NotImplementedException();
        }

        public override void Activate()
        {
            throw new System.NotImplementedException();
        }

        public override void Deactivate()
        {
            throw new System.NotImplementedException();
        }
    }
}