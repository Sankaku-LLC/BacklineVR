using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TargetType
{
    None = 0, Player = 1, Ally = 2, Enemy = 3
}
public interface  ITargetable 
{
    public GameObject GetGameObject();
    public Transform GetMainTransform();
    public TargetType GetTargetType();
}
