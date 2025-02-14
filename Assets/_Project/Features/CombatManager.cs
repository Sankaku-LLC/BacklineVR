using CurseVR.Director;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CombatManager : MonoBehaviour, IGlobalComponent
{
    public readonly List<ITargetable> AllSpawnedEnemies = new List<ITargetable>(32);
    public readonly List<ITargetable> AllSpawnedAllies = new List<ITargetable>(32);
    public void OnInitialize()
    {
    }

    public void OnStart()
    {
    }

    public Type GetManagerType()
    {
        return typeof(CombatManager);
    }
    public void OnEnemySpawned(ITargetable enemy)
    {
        AllSpawnedEnemies.Add(enemy);
    }
    public void OnAllySpawned(ITargetable ally) { 
        AllSpawnedAllies.Add(ally);
    }
    public void OnEnemyKilled(ITargetable enemy)
    {
        AllSpawnedEnemies.Remove(enemy);
    }
    public void OnAllyKilled(ITargetable ally)
    {
        AllSpawnedAllies.Remove(ally);
    }
    public ITargetable GetClosestOfType(TargetType targetType, Vector3 curPosition)
    {
        switch (targetType)
        {
            case TargetType.Enemy:
                return GetClosestEnemyUnit(curPosition);
            case TargetType.Ally:
                return GetClosestAllyUnit(curPosition);
            case TargetType.Player:
                break;
            default:
                break;
        }
        return null;
    }
    public ITargetable GetClosestAllyUnit(Vector3 curPosition)
    {
        ITargetable closestAlly = null;
        var closestDist = float.MaxValue;
        foreach (var ally in AllSpawnedAllies)
        {
            var newDist = GetSquaredDistance(ally.GetMainTransform().position, curPosition);
            if (IsLessThanAndNonZero(newDist, closestDist))
            {
                closestDist = newDist;
                closestAlly = ally;
            }
        }
        return closestAlly;
    }

    public ITargetable GetClosestEnemyUnit(Vector3 curPosition)
    {
        ITargetable closestEnemy = null;
        var closestDist = float.MaxValue;
        foreach(var enemy in AllSpawnedEnemies)
        {
            var newDist = GetSquaredDistance(enemy.GetMainTransform().position, curPosition);
            if(IsLessThanAndNonZero(newDist, closestDist))
            {
                closestDist = newDist;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }
    public static float GetSquaredDistance(Vector3 firstPoint, Vector3 secondPoint)
    {
        return FlattenY(firstPoint - secondPoint).sqrMagnitude;
    }
    private static Vector3 FlattenY(Vector3 input)
    {
        var newValue = new Vector3(input.x, 0, input.z);
        return newValue;
    }

    private bool IsLessThanAndNonZero(float newNumber, float currentNumber)
    {
        return !Mathf.Approximately(newNumber, 0) && newNumber < currentNumber;
    }
}
