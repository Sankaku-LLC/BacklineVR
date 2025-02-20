using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CurseVR.UI {
    public class UIGroup : MonoBehaviour
    {
        [Serializable]
        public struct UIMemberListing
        {
            public string Name;
            public UIBehaviour Behaviour;
        }
        [SerializeField]
        private UIMemberListing[] _members;
        private Dictionary<string, UIBehaviour> _memberDict;

        [Serializable]
        public struct UIGroupListing
        {
            public string Name;
            public UIGroup Group;
        }
        [SerializeField]
        private UIGroupListing[] _subgroups;
        private Dictionary<string, UIGroup> _groupDict;

        private bool _initialized = false;

        public void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;

                _memberDict = new Dictionary<string, UIBehaviour>(_members.Length);
                foreach (var member in _members)
                {
                    if (!_memberDict.ContainsKey(member.Name))
                        _memberDict.Add(member.Name, member.Behaviour);
                    else
                        Debug.LogError("UI Member " + member.Name + " already added!");
                }

                _groupDict = new Dictionary<string, UIGroup>(_subgroups.Length);
                foreach (var group in _subgroups)
                {
                    if (!_groupDict.ContainsKey(group.Name))
                    {
                        _groupDict.Add(group.Name, group.Group);
                        group.Group.Initialize();
                    }
                    else
                        Debug.LogError("UI Group " + group.Name + " already added!");
                }
            }
        }

        #region Get and Set
        public UIGroup GetSubgroup(string name)
        {
            if (!_groupDict.ContainsKey(name))
            {
                Debug.LogError("UI Subgroup " + name + " in " + gameObject.name + " not found!");
                return null;
            }
            else
            {
                return _groupDict[name];
            }
        }
        public UIBehaviour GetMember(string name)
        {
            if (!_memberDict.ContainsKey(name))
            {
                Debug.LogError("UI Group member " + name + " in " + gameObject.name + " not found!");
                return null;
            }
            else
            {
                return _memberDict[name];
            }
        }
        /// <summary>
        /// Will get the parameter associated with the UI component and return it as an object. It is the user's responsibility to cast the result appropriately
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object Get(string name)
        {
            var member = _memberDict[name];
            switch (member)
            {
                case Dropdown d:
                    return d.value;
                case Toggle t:
                    return t.isOn;
                case TMPro.TMP_Text t:
                    return t.text;
                case Slider s:
                    return s.value;
                default:
                    Debug.LogError("Get for " + name + " of type string does not exist!");
                    return null;
            }
        }
        public void SetSubgroupActive(string name, bool state)
        {
            var subgroup = GetSubgroup(name);
            if (subgroup != null)
                subgroup.SetGroupActive(state);
        }
        public void SetGroupActive(bool state)
        {
            gameObject.SetActive(state);
        }
        public void Select(string name)
        {
            var member = _memberDict[name];
            if (member is Selectable s)
            {
                s.Select();
            }
            else
            {
                Debug.LogError(name + " is not a selectable");
            }
        }

        public void SetActive(string name, bool state)
        {
            _memberDict[name].gameObject.SetActive(state);
        }

        public void SetInteractable(string name, bool state)
        {
            var selectable = GetSelectable(name);
            if (selectable != null)
            {
                selectable.interactable = state;
            }
        }

        public void Set(string name, object value)
        {
            var member = _memberDict[name];
            switch (member)
            {
                case Button button:
                    if (value is string label)
                    {
                        button.GetComponentInChildren<TMPro.TMP_Text>().text = label;
                        return;
                    }
                    else if (value is Sprite sprite)
                    {
                        button.image.sprite = sprite;
                        return;
                    }
                    break;
                case Dropdown dropdown:
                    if (value is List<Dropdown.OptionData> data)
                    {
                        dropdown.options = data;
                        return;
                    }
                    else if (value is int i)
                    {
                        dropdown.value = i;
                        return;
                    }
                    break;
                case Toggle toggle:
                    if (value is bool b)
                    {
                        toggle.isOn = b;
                        return;
                    }
                    break;
                case TMPro.TMP_Text text:
                    if (value is string s)
                    {
                        text.text = s;
                        return;
                    }
                    break;
                case Slider slider:
                    if (value is float f)
                    {
                        slider.value = f;
                        return;
                    }
                    break;
                case Image image:
                    if (value is Color c)
                    {
                        image.color = c;
                        return;
                    }
                    else if (value is Sprite sprite)
                    {
                        image.sprite = sprite;
                        return;
                    }
                    break;
            }
            Debug.LogError(name + " did not have any value set, check value type");
        }

        #endregion
        #region Listeners
        public void ListenerAction(string name, UnityAction action, bool add)
        {
            var selectable = GetSelectable(name);
            if (selectable != null)
            {
                switch (selectable)
                {
                    case Button b:
                        if (add)
                            b.onClick.AddListener(action);
                        else
                            b.onClick.RemoveListener(action);
                        break;
                    default:
                        Debug.LogError("Selectable of type " + typeof(Selectable) + " does not have a corresponding action<void>!");
                        break;
                }
            }
        }
        public void ListenerAction(string name, UnityAction<bool> action, bool add)
        {
            var selectable = GetSelectable(name);
            if (selectable != null)
            {
                switch (selectable)
                {
                    case Toggle t:
                        if (add)
                            t.onValueChanged.AddListener(action);
                        else
                            t.onValueChanged.RemoveListener(action);
                        break;
                    default:
                        Debug.LogError("Selectable of type " + typeof(Selectable) + " does not have a corresponding action<bool>!");
                        break;
                }
            }
        }
        public void ListenerAction(string name, UnityAction<string> action, bool add)
        {
            var selectable = GetSelectable(name);
            if (selectable != null)
            {
                switch (selectable)
                {
                    case Dropdown d:
                        if (add)
                            d.onValueChanged.AddListener((value) => action?.Invoke(d.options[value].text));
                        else
                            d.onValueChanged.RemoveListener((value) => action?.Invoke(d.options[value].text));
                        break;
                    case InputField i:
                        if (add)
                            i.onEndEdit.AddListener(action);
                        else
                            i.onEndEdit.RemoveListener(action);
                        break;
                    default:
                        Debug.LogError("Selectable of type " + typeof(Selectable) + " does not have a corresponding action<string>!");
                        break;
                }
            }
        }
        public void ListenerAction(string name, UnityAction<float> action, bool add)
        {
            var selectable = GetSelectable(name);
            if (selectable != null)
            {
                switch (selectable)
                {
                    case Slider s:
                        if (add)
                            s.onValueChanged.AddListener(action);
                        else
                            s.onValueChanged.RemoveListener(action);
                        break;
                    default:
                        Debug.LogError("Selectable of type " + typeof(Selectable) + " does not have a corresponding action<float>!");
                        break;
                }
            }
        }
        #endregion
        Selectable GetSelectable(string name)
        {
            var member = _memberDict[name];
            if (member is Selectable s)
            {
                return s;
            }
            else
            {
                Debug.LogError(name + " is not a selectable");
                return null;
            }
        }
    }

}
