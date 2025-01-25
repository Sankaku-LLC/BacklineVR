using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BacklineVR.Interaction
{
    public interface Interactable
    {
        public void OnGrab();
        public void OnRelease();
        public Pose GetGrabPose();
        public void OnActivate();
        public void OnDeactivate();
        public void OnUpdate();
    }
}