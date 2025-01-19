using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Characters
{
    public interface IDestructible
    {
        public float GetHealth();
        public void SetHealth(float health);
        public void DealDamage();
    }
}