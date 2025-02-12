using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    public enum SpellAttribute { Fire }
    public enum CastingVariant { Point, Ray, Entity}

    /// <summary>
    /// Spells begin their lifecycle after being spawned by a targeting system. From there they can do whatever they want - fly forwards, explode, apply StatusEffect scripts, etc
    /// </summary>
    public abstract class Spell : MonoBehaviour
    {
        [SerializeField]
        private protected SpellAttribute _attribute;
    }
}