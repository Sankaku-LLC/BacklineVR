using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllyCharacter : GenericCharacter, ITargetable
{
    
    public override TargetType GetTargetType() => TargetType.Ally;

    public override void OnInitialized()
    {
        _combatManager.OnAllySpawned(this);
    }
}
