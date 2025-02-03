using BacklineVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    public abstract class Item : Holdable
    {
        public abstract string GetCode();
        public abstract void Activate();
        public abstract void Deactivate();
    }
}