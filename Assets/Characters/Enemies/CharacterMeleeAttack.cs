using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BacklineVR.Characters;

namespace BehaviorDesigner.Runtime.Tasks
{
    public class MeleeAttack : GenericCharacterAction
    {
        public override TaskStatus OnUpdate()
        {
            _character.Target.GetGameObject().GetComponent<IDestructible>().TakeDamage(_character.GetMeleeAttackDamage());
            return TaskStatus.Success;
        }
    }
}