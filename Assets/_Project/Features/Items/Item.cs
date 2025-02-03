using BacklineVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// An item is something that can be grabbed, released, activated, deactivated, and bound to a grab cast in the inventory
    /// </summary>
    public abstract class Item : Holdable
    {
        public abstract string GetCode();
        public abstract void Activate();
        public abstract void Deactivate();
    }
}