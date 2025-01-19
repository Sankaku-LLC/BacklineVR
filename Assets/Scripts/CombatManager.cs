using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;


public class CombatManager : MonoBehaviour
{
    public static CombatManager CombatManagerInstance;
    public readonly List<EnemyCharacter> AllSpawnedEnemies = new List<EnemyCharacter>(32);
    public readonly List<AllyCharacter> AllSpawnedAllies = new List<AllyCharacter>(32);
    void Awake()
    {
        if (CombatManagerInstance)
        {
            Destroy(gameObject);
            return;
        }
        CombatManagerInstance = this;

    }

    private void Start()
    {
        DontDestroyOnLoad(CombatManagerInstance.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnEnemySpawned(EnemyCharacter enemy)
    {
        AllSpawnedEnemies.Add(enemy);
    }
    public void OnAllySpawned(AllyCharacter ally) { 
        AllSpawnedAllies.Add(ally);
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
        AllyCharacter closestAlly = null;
        var closestDist = float.MaxValue;
        foreach (var ally in AllSpawnedAllies)
        {
            var newDist = GetSquaredDistance(ally.transform.position, curPosition);
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
        EnemyCharacter closestEnemy = null;
        var closestDist = float.MaxValue;
        foreach(var enemy in AllSpawnedEnemies)
        {
            var newDist = GetSquaredDistance(enemy.transform.position, curPosition);
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
