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
        public void TakeDamage(float damageAmount, bool forceCrit)
        {
        }
        public void RecoverHealth(float healAmount)
        {
        }
        public bool IsDestroyed();
        public bool IsStaggered();
    }
}