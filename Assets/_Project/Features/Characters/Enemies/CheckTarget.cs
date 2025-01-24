using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
namespace BehaviorDesigner.Runtime.Tasks
{
    public class CheckTarget : GenericCharacterAction
    {
        public override TaskStatus OnUpdate()
        {
            if(_character.Target != null)
                return TaskStatus.Success;

            return TaskStatus.Failure;
        }
    }
    
}
