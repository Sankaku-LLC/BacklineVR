using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BehaviorDesigner.Runtime.Tasks
{
    public class MoveToTarget : GenericCharacterAction
    {
        public override TaskStatus OnUpdate()
        {
            _character.StartMovement();
            return TaskStatus.Success;
        }
    }
}