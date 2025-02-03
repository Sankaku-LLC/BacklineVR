using CurseVR.Director;
using CurseVR.SymbolSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BacklineVR.Items
{
    /// <summary>
    /// Inventory system manager
    /// </summary>
    public class ItemManager : MonoBehaviour, IGlobalComponent
    {
        private readonly Dictionary<string, Item> _inventory = new Dictionary<string, Item>();

        [SerializeField]
        private List<Item> _items;

        [SerializeField]
        private List<string> _throwableItems;
        [SerializeField]
        private List<string> _magicItems;

        public Type GetManagerType() => typeof(ItemManager);

        public void OnInitialize()
        {
        }

        public void OnStart()
        {
            foreach(var item in _throwableItems)
            {
                SpawnItem(false, item);
            }
            foreach(var item in _magicItems)
            {
                SpawnItem(false, item);
            }
        }
        public Item SpawnItem(bool shouldHide, string itemCode)
        {
            var itemTemplate = _items.First(x => x.GetCode() == itemCode);
            if(itemTemplate == null)
                return null;
            var spawnedItem = Instantiate(itemTemplate);
            spawnedItem.enabled = shouldHide;
            return spawnedItem;
        }
        public bool TryGetItem(SymbolData symbolData, bool isDominant, out Item item)
        {
            if (isDominant)
            {
             //   foreach(var inventoryItem in _inventory)
            }
            item = null;
            return false;
        }
    }
}