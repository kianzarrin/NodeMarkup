﻿using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class MessageBoxBase : UIPanel
    {
        protected static float Width { get; } = 573;
        protected static float Height { get; } = 200;
        protected static float ButtonHeight { get; } = 47;
        protected static float Padding { get; } = 16;
        private static float MaxContentHeight { get; } = 500;

        public static T ShowModal<T>()
        where T : MessageBoxBase
        {
            var uiObject = new GameObject();
            uiObject.transform.parent = UIView.GetAView().transform;
            var messageBox = uiObject.AddComponent<T>();

            UIView.PushModal(messageBox);
            messageBox.Show(true);
            messageBox.Focus();

            var view = UIView.GetAView();

            if (view.panelsLibraryModalEffect != null)
            {
                view.panelsLibraryModalEffect.FitTo(null);
                if (!view.panelsLibraryModalEffect.isVisible || view.panelsLibraryModalEffect.opacity != 1f)
                {
                    view.panelsLibraryModalEffect.Show(false);
                    ValueAnimator.Animate("ModalEffect67419", delegate (float val)
                    {
                        view.panelsLibraryModalEffect.opacity = val;
                    }, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
                }
            }

            return messageBox;
        }
        public static void HideModal(MessageBoxBase messageBox)
        {
            UIView.PopModal();

            var view = UIView.GetAView();
            if (view.panelsLibraryModalEffect != null)
            {
                if (!UIView.HasModalInput())
                {
                    ValueAnimator.Animate("ModalEffect67419", delegate (float val)
                    {
                        view.panelsLibraryModalEffect.opacity = val;
                    }, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), delegate ()
                    {
                        view.panelsLibraryModalEffect.Hide();
                    });
                }
                else
                {
                    view.panelsLibraryModalEffect.zOrder = UIView.GetModalComponent().zOrder - 1;
                }
            }

            messageBox.Hide();
            messageBox.ClearScrollableContent();
            Destroy(messageBox.gameObject);
        }

        public string CaprionText { set => Caption.text = value; }

        private UILabel Caption { get; set; }
        protected UIPanel ButtonPanel { get; private set; }
        protected UIScrollablePanel ScrollableContent { get; private set; }
        private UIDragHandle Handle { get; set; }

        public MessageBoxBase()
        {
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = Width;
            height = Height;
            color = new Color32(58, 88, 104, 255);
            backgroundSprite = "MenuPanel";

            AddHandle();
            AddPanel();
            FillContent();
            AddButtonPanel();
            Init();

            ScrollableContent.eventSizeChanged += ContentSizeChanged;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));
        }

        private void AddHandle()
        {
            Handle = AddUIComponent<UIDragHandle>();
            Handle.size = new Vector2(Width, 42);
            Handle.relativePosition = new Vector2(0, 0);
            //Handle.target = parent;
            Handle.eventSizeChanged += (component, size) =>
            {
                Caption.size = size;
                Caption.CenterToParent();
            };

            Caption = Handle.AddUIComponent<UILabel>();
            Caption.textAlignment = UIHorizontalAlignment.Center;
            Caption.textScale = 1.3f;
            Caption.anchor = UIAnchorStyle.Top;

            Caption.eventTextChanged += (component, text) => Caption.CenterToParent();

            var cancel = Handle.AddUIComponent<UIButton>();
            cancel.normalBgSprite = "buttonclose";
            cancel.hoveredBgSprite = "buttonclosehover";
            cancel.pressedBgSprite = "buttonclosepressed";
            cancel.size = new Vector2(32, 32);
            cancel.relativePosition = new Vector2(527, 4);
            cancel.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Cancel();
        }
        private void AddPanel()
        {
            ScrollableContent = AddUIComponent<UIScrollablePanel>();
            ScrollableContent.width = Width;
            ScrollableContent.autoLayout = true;
            ScrollableContent.autoLayoutDirection = LayoutDirection.Vertical;
            ScrollableContent.autoLayoutPadding = new RectOffset((int)Padding, (int)Padding, 0, 0);
            ScrollableContent.clipChildren = true;
            ScrollableContent.builtinKeyNavigation = true;
            ScrollableContent.scrollWheelDirection = UIOrientation.Vertical;
            ScrollableContent.maximumSize = new Vector2(Width, MaxContentHeight);
            UIUtils.AddScrollbar(this, ScrollableContent);

            ScrollableContent.eventComponentAdded += (UIComponent container, UIComponent child) => AddChildHandles(child);
            ScrollableContent.eventComponentRemoved += (UIComponent container, UIComponent child) => RemoveChildHandles(child);
        }
        private void ClearScrollableContent()
        {
            if (ScrollableContent == null)
                return;

            foreach(var item in ScrollableContent.components)
                RemoveChildHandles(item);
        }
        private void AddChildHandles(UIComponent child)
        {
            child.eventVisibilityChanged += OnChildVisibilityChanged;
            child.eventSizeChanged += OnChildSizeChanged;
            child.eventPositionChanged += OnChildPositionChanged;
        }
        private void RemoveChildHandles(UIComponent child)
        {
            child.eventVisibilityChanged -= OnChildVisibilityChanged;
            child.eventSizeChanged -= OnChildSizeChanged;
            child.eventPositionChanged -= OnChildPositionChanged;
        }
        private void OnChildVisibilityChanged(UIComponent component, bool value) => FitContentChildren();
        private void OnChildSizeChanged(UIComponent component, Vector2 value) => FitContentChildren();
        private void OnChildPositionChanged(UIComponent component, Vector2 value) => FitContentChildren();

        private void FitContentChildren()
        {
            ScrollableContent.FitChildrenVertically();
            ScrollableContent.width = ScrollableContent.verticalScrollbar?.isVisible == true ? Width - ScrollableContent.verticalScrollbar.width : Width;
        }
        private void ContentSizeChanged(UIComponent component, Vector2 value) => Init();
        private void Init()
        {
            height = Handle.height + ScrollableContent.height + ButtonPanel.height + Padding;
            ScrollableContent.relativePosition = new Vector2(0, Handle.height);
            ButtonPanel.relativePosition = new Vector2(0, Handle.height + ScrollableContent.height + Padding);
            ScrollableContent.verticalScrollbar.relativePosition = ScrollableContent.relativePosition + new Vector3(ScrollableContent.width, 0);
            ScrollableContent.verticalScrollbar.height = ScrollableContent.height;

            foreach (var item in ScrollableContent.components)
            {
                item.width = ScrollableContent.width - 2 * Padding;
            }
        }
        protected virtual void FillContent() { }
        private void AddButtonPanel()
        {
            ButtonPanel = AddUIComponent<UIPanel>();
            ButtonPanel.size = new Vector2(Width, ButtonHeight + 10);
        }
        protected UIButton AddButton(int i, int from, Action action)
        {
            var width = (this.width - (25 * (from + 1))) / from;
            var button = ButtonPanel.AddUIComponent<UIButton>();
            button.normalBgSprite = "ButtonMenu";
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.horizontalAlignment = UIHorizontalAlignment.Center;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.size = new Vector2(width, ButtonHeight);
            button.relativePosition = new Vector2(width * (i - 1) + 25 * i, 0);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => action?.Invoke();
            return button;
        }
        protected override void OnKeyDown(UIKeyEventParameter p)
        {
            if (!p.used)
            {
                if (p.keycode == KeyCode.Escape)
                {
                    p.Use();
                    Cancel();
                }
                else if (p.keycode == KeyCode.Return)
                {
                    p.Use();
                }
            }
        }

        protected virtual void Cancel() => HideModal(this);
    }
}
