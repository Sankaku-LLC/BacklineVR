using CurseVR.Director;
using CurseVR.SymbolSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// Inventory system manager
    /// </summary>
    public class ItemManager : MonoBehaviour, IGlobalComponent
    {
        [SerializeField]
        private List<Item> _items;
        [SerializeField]
        private List<Item> _equipment;
        public Type GetManagerType() => typeof(ItemManager);

        public void OnInitialize()
        {
        }

        public void OnStart()
        {
        }
        public bool TryGetItem(SymbolData symbolData, out Item item)
        {
            item = null;
            return false;
        }
    }
}