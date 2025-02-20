using CurseVR.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.UI
{
    /// <summary>
    /// This will dynamically generate a vertical list of items
    /// </summary>
    public class ItemScrollContainer : UIContainer
    {
        private const string KEY_SCROLLVIEW = "ScrollView";
        private const float DIST_FROM_PLAYER = 3;//1.135f;

        [SerializeField]
        private UIGroup _itemGroup;
        [SerializeField]
        private GameObject _entryPrefab;
        [SerializeField]
        private GameObject _listParent;

        public int _selectedIdx;
        private List<GameObject> _entries;
        public override Type GetCanvasType() => typeof(ItemScrollContainer);

        public override void BaseInitialize()
        {
            _itemGroup.Initialize();
        }
        public override void BaseShow()
        {
            UpdateInterfacePose();
        }

        public override void BaseHide()
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                Destroy(_entries[i]);
            }
            _listParent.SetActive(false);
        }


        private void UpdateInterfacePose()
        {
            transform.rotation = Quaternion.Euler(Vector3.Scale(Camera.main.transform.rotation.eulerAngles, Vector3.up));
            transform.position = Vector3.Scale(Camera.main.transform.position + Camera.main.transform.forward * DIST_FROM_PLAYER, new Vector3(1, 0, 1)) + Camera.main.transform.position.y * Vector3.up;
        }
        public void SetEntries(List<string> entries)
        {
            foreach (var entry in entries)
            {
                _entries.Add(Instantiate(_entryPrefab, _listParent.transform));
            }
        }
        public string GetSelectedEntry()
        {
            return _entries[_selectedIdx].name;
        }
        public bool TryScroll(bool scrollUp)
        {
            var delta = scrollUp ? -1 : 1;
            var newIdx = _selectedIdx + delta;
            if (newIdx < 0 || newIdx >= _entries.Count)
                return false;
            _selectedIdx = newIdx;

            return true;
        }

    }
}