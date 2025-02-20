using BacklineVR.Core;
using CurseVR.Director;
using CurseVR.SymbolSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BacklineVR.Items
{
    public enum ItemCategory { Equipment = 0, Item = 1, Scrolls = 2 }
    /// <summary>
    /// Inventory system manager
    /// </summary>
    public class ItemManager : MonoBehaviour, IGlobalComponent
    {
        //Temporary method of storing list of all items templates that can be spawned. Will be list of prefabs, maybe a scriptableObject
        [SerializeField]
        private List<GameObject> _catalog;

        //Temporary method of storing what items the player had at start
        [SerializeField]
        private List<ItemFrequencyPair<string>> _equipment;
        [SerializeField]
        private List<ItemFrequencyPair<string>> _items;
        [SerializeField]
        private List<ItemFrequencyPair<string>> _scrolls;


        //List of player item codes and their frequency, populated at runtime
        private readonly Dictionary<string, int> _inventory = new Dictionary<string, int>(32);
        //List of spawnable items, generated at start for spawning
        private readonly Dictionary<string, GameObject> _itemTemplates = new Dictionary<string, GameObject>(32);

        public Type GetManagerType() => typeof(ItemManager);

        public void OnInitialize()
        {
            foreach(var item in _catalog)
            {
                _itemTemplates.Add(item.GetComponent<Item>().GetCode(), item);
            }
        }

        public void OnStart()
        {
            foreach(var item in _items)
            {
                _inventory.Add(item.Item, item.Frequency);
            }
            foreach(var item in _scrolls)
            {
                _inventory.Add(item.Item, item.Frequency);
            }
        }
        public List<ItemFrequencyPair<string>> GetItemsOfCategory(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Equipment:
                    return _equipment;
                case ItemCategory.Item:
                    return _items;
                case ItemCategory.Scrolls:
                    return _scrolls;
                default:
                    return null;
            }
        }
        public bool TryGetItem(string itemCode, out Item item)
        {
            if (!_inventory.ContainsKey(itemCode))
            {
                Debug.LogError("Item " + itemCode + " is not in inventory!");
                item = null;
                return false;
            }
            if (_inventory[itemCode] == 0)
            {
                Debug.LogError("No more of item " + itemCode + " in inventory!");
                item = null;
                return false;
            }
            item = Instantiate(_itemTemplates[itemCode]).GetComponent<Item>();
            _inventory[itemCode] -= 1;
            return true;
        }
        public void StoreItem(Item item, int count = 1)
        {
            var itemCode = item.GetCode();
            if (!_inventory.ContainsKey(itemCode))
            {
                _inventory.Add(itemCode, 0);
            }
            _inventory[itemCode] += count;//TODO: Add a way to save/ load inventory contents
            Destroy(item.gameObject);
        }
    }
}