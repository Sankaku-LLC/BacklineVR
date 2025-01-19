using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks
{
    public class GenericCharacterAction : Action
    {
        public  GenericCharacter _character { private set; get; }
        public override void OnStart()
        {
           _character = GetComponent<GenericCharacter>();
        }
       
    }
}