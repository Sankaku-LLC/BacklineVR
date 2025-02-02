using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    public abstract class Item : MonoBehaviour
    {
        public abstract void Activate();
        public abstract void Deactivate();
    }
}