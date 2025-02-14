using BacklineVR.Interaction;
using System;
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
    [RequireComponent(typeof(Throwable))]
    public abstract class ThrowableItem : Item
    {
        [SerializeField]
        private string _throwableItemCode;
        private Throwable _throwable;
        private float _countDown;
        public bool CanCatch => _throwable.CanCatch();
        public override string GetCode() => _throwableItemCode;
        private void Awake()
        {
            _throwable = GetComponent<Throwable>();
            _throwable.OnThrowItem += OnItemThrown;
        }
        private void Start()
        {
        }
        public override bool ShouldReparent()
        {
            return !_throwable.FastEnough();
        }
        private protected abstract void OnItemThrown();
        private void OnCollisionEnter(Collision collision)
        {

        }
        private protected abstract void OnCollision();
    }
}