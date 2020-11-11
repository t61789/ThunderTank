using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Tool
{
    public class PanelUI:BaseUI, ICanvasRaycastFilter
    {
        public bool Stable = false;

        [HideInInspector] public UnityEvent<PanelUI> OnOpen = new UnityEvent<PanelUI>();
        [HideInInspector] public UnityEvent<PanelUI> OnClose = new UnityEvent<PanelUI>();
        [HideInInspector] public UnityEvent<PanelUI, Action> OnCloseCheck = new UnityEvent<PanelUI, Action>();
        [HideInInspector] public PanelUI Dialog;

        protected virtual void AfterOpen()
        {

        }

        protected virtual void BeforeClose()
        {

        }

        public bool CloseCheck()
        {
            var result = true;
            OnCloseCheck?.Invoke(this, () => result = false);
            if (!result)
                return false;

            OnClose?.Invoke(this);
            return true;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (Dialog == null) return true;
            return !Dialog.gameObject.activeSelf;
        }
    }
}
