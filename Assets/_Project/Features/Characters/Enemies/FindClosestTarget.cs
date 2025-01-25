using CurseVR.Director;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks
{
    public class FindClosestTarget : GenericCharacterAction
    {
        private CombatManager _combatManager;
        public TargetType targetType;

        public override void OnAwake()
        {
            _combatManager = GlobalDirector.Get<CombatManager>();
        }

        public override TaskStatus OnUpdate()
        {
            var target = _combatManager.GetClosestOfType(targetType, transform.position);
            if (target != null) { 
                _character.SetTarget(target);
                return TaskStatus.Success;
            }
            return TaskStatus.Failure;
        }
    }
}