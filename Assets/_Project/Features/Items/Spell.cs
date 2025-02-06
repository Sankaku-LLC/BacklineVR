using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    public enum SpellAttribute { Fire }
    public enum CastingVariant { Point, Ray, Entity}
    public class Spell : MonoBehaviour
    {
        public void Activate(Vector3 position)
        {

        }
        public void Activate(List<ITargetable> targets)
        {

        }
        public void Activate(List<Ray> rays)
        {

        }
    }
}