using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BehaviorDesigner.Runtime.Tasks
{
    public class CheckTargetWithinReach : GenericCharacterAction
    {
        public override TaskStatus OnUpdate()
        {
            if(_character.DistanceToTarget() > _character.MeleeReach)
                return TaskStatus.Running;

            return TaskStatus.Success;
        }
    }
}