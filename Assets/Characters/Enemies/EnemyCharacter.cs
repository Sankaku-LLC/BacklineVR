using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCharacter : GenericCharacter, ITargetable
{
    public override TargetType GetTargetType() => TargetType.Enemy;
    public override float MeleeReach => 1f;
    public override void OnInitialized()
    {
        _combatManager.OnEnemySpawned(this);
    }
    public override void OnKilled()
    {
        base.OnKilled();
        _combatManager.OnEnemyKilled(this);
    }
}
