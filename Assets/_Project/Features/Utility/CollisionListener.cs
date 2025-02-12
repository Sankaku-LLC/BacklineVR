using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Core
{
    public class CollisionListener : MonoBehaviour
    {
        public Action<Collider> OnTriggerStart;
        public Action<Collider> OnTriggerEnd;
        private void OnTriggerEnter(Collider other)
        {
            OnTriggerStart?.Invoke(other);
        }
        private void OnTriggerExit(Collider other)
        {
            OnTriggerEnd?.Invoke(other);
        }
    }
}