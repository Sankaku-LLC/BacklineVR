using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Characters
{
    public interface IDestructible
    {
        public void TakeDamage(float damageAmount)
        {
        }
        public void RecoverHealth(float healAmount)
        {
        }
        public bool IsDestroyed();
    }
}