using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CurseVR.UI
{
    public interface IContainer
    {
        /// <summary>
        /// Called at start
        /// </summary>
        public void Initialize();
        public Type GetCanvasType();
        public void Show();
        public void Hide();
    }
    [RequireComponent(typeof(Canvas))]
    public abstract class UIContainer : MonoBehaviour, IContainer
    {
        public bool ShowAtStart = false;

        private Canvas _canvas;
        public void Initialize()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.worldCamera = Camera.main;
            BaseInitialize();
        }
        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Debug.LogWarning("Attempting to show but gameOBject is disabled, Canvas component itself should be");
            }
            _canvas.enabled = true;
            BaseShow();
        }
        public void Hide()
        {
            _canvas.enabled = false;
            BaseHide();
        }
        public abstract void BaseShow();
        public abstract void BaseHide();
        public abstract void BaseInitialize();
        public abstract Type GetCanvasType();
    }
}