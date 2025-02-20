using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CurseVR.Director;
using CurseVR.UI;
using UnityEngine;

public class ContainerManager : MonoBehaviour, IGlobalComponent
{
    public Action OnContainersInitialized;
    private Dictionary<Type, UIContainer> _containerMap;
    public Type GetManagerType() => typeof(ContainerManager);
    public void OnInitialize()
    {
        var childContainers = FindObjectsOfType<UIContainer>(true);
        _containerMap = new Dictionary<Type, UIContainer>(childContainers.Length);
        foreach (var child in childContainers)
        {
            if (_containerMap.ContainsKey(child.GetType()))
            {
                Debug.LogWarning("Attempted to add same key: " + child.GetType());
                continue;
            }
            _containerMap.Add(child.GetCanvasType(), child);
            child.Initialize();
        }
        foreach (var container in _containerMap.Values)
        {
            if (container.ShowAtStart)
                container.Show();
            else
                container.Hide();
        }
        OnContainersInitialized?.Invoke();
    }
    public void OnStart()
    {

    }
    public T GetContainer<T>() where T : UIContainer
    {
        return (T)_containerMap[typeof(T)];
    }
    public List<UIContainer> GetAllContainers()
    {
        return _containerMap.Values.ToList();
    }
    public void Show(Type toShow)
    {
        if(_containerMap.TryGetValue(toShow, out UIContainer container))
        {
            container.Show();
            return;
        }
        Debug.LogError("Container " + toShow + " is not registered!");
    }
    public void Hide(Type toHide)
    {
        if (_containerMap.TryGetValue(toHide, out UIContainer container))
        {
            container.Hide();
            return;
        }
        Debug.LogError("Container " + toHide + " is not registered!");
    }
    public void HideAllExcept(Type toShow)
    {
        foreach(var container in _containerMap)
        {
            if(container.Key == toShow)
            {
                container.Value.Show();
            }
            else
            {
                container.Value.Hide();
            }
        }
    }
}
