using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    public enum SpellAttribute { Fire }
    public enum CastingVariant { Point, Ray, Entity}
    public abstract class Spell : MonoBehaviour
    {
        [SerializeField]
        private protected GameObject _spellPrefab;
        [SerializeField]
        private protected SpellAttribute _attribute;
        public virtual void Activate(Vector3 position)
        {

        }
        public virtual void Activate(List<ITargetable> targets)
        {

        }
        public virtual void Activate(List<Ray> rays)
        {

        }
    }
}