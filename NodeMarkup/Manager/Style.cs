﻿using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IStyle { }
    public interface IColorStyle : IStyle
    {
        Color32 Color { get; set; }
    }
    public interface IWidthStyle: IStyle
    {
        float Width { get; set; }
    }
    public abstract class Style : IToXml
    {
        public static bool FromXml<T>(XElement config, out T style) where T : Style
        {
            var type = IntToType(config.GetAttrValue<int>("T"));

            if (TemplateManager.GetDefault<T>(type) is T defaultStyle)
            {
                style = defaultStyle;
                style.FromXml(config);
                return true;
            }
            else
            {
                style = default;
                return false;
            }
        }
        private static StyleType IntToType(int rawType)
        {
            var typeGroup = rawType & (int)StyleType.GroupMask;
            var typeNum = (rawType & (int)StyleType.ItemMask) + 1;
            var type = (StyleType)((typeGroup == 0 ? (int)StyleType.RegularLine : typeGroup << 1) + typeNum);
            return type;
        }
        private static int TypeToInt(StyleType type)
        {
            var typeGroup = (int)type & (int)StyleType.GroupMask;
            var typeNum = ((int)type & (int)StyleType.ItemMask) - 1;
            var rawType = ((typeGroup >> 1) & (int)StyleType.GroupMask) + typeNum;
            return rawType;
        }

        public static Color32 DefaultColor { get; } = new Color32(136, 136, 136, 224);
        public static float DefaultWidth { get; } = 0.15f;

        public static T GetDefault<T>(StyleType type) where T : Style
        {
            switch (type & StyleType.GroupMask)
            {
                case StyleType.RegularLine when RegularLineStyle.GetDefault((RegularLineStyle.RegularLineType)(int)type) is T tStyle:
                    return tStyle;
                case StyleType.StopLine when StopLineStyle.GetDefault((StopLineStyle.StopLineType)(int)type) is T tStyle:
                    return tStyle;
                case StyleType.Filler when FillerStyle.GetDefault((FillerStyle.FillerType)(int)type) is T tStyle:
                    return tStyle;
                default:
                    return null;
            }
        }
        public static string GetShortName(StyleType type)
        {
            switch (type)
            {
                case StyleType.LineSolid: return Localize.LineStyle_SolidShort;
                case StyleType.LineDashed: return Localize.LineStyle_DashedShort;
                case StyleType.LineDoubleSolid: return Localize.LineStyle_DoubleSolidShort;
                case StyleType.LineDoubleDashed: return Localize.LineStyle_DoubleDashedShort;
                case StyleType.LineSolidAndDashed: return Localize.LineStyle_SolidAndDashedShort;
                case StyleType.StopLineSolid: return Localize.LineStyle_StopShort;
                case StyleType.StopLineDashed: return Localize.LineStyle_StopDashedShort;
                case StyleType.StopLineDoubleSolid: return Localize.LineStyle_StopDoubleShort;
                case StyleType.StopLineDoubleDashed: return Localize.LineStyle_StopDoubleDashedShort;
                case StyleType.FillerStripe: return Localize.FillerStyle_StripeShort;
                case StyleType.FillerGrid: return Localize.FillerStyle_GridShort;
                case StyleType.FillerSolid: return Localize.FillerStyle_SolidShort;
                default: return null;
            }
        }

        public static string XmlName { get; } = "S";

        public Action OnStyleChanged { private get; set; }
        public string XmlSection => XmlName;
        public abstract StyleType Type { get; }

        protected virtual void StyleChanged() => OnStyleChanged?.Invoke();

        Color32 _color;
        float _width;

        public Color32 Color
        {
            get => _color;
            set
            {
                _color = value;
                StyleChanged();
            }
        }
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                StyleChanged();
            }
        }
        public Style(Color32 color, float width)
        {
            Color = color;
            Width = width;
        }
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", TypeToInt(Type)),
                new XAttribute("C", Color.ToInt()),
                new XAttribute("W", Width)
            );
            return config;
        }
        public virtual void FromXml(XElement config)
        {
            var colorInt = config.GetAttrValue<int>("C");
            Color = colorInt != 0 ? colorInt.ToColor() : DefaultColor;
            Width = config.GetAttrValue("W", DefaultWidth);
        }

        public abstract Style Copy();
        public virtual void CopyTo(Style target)
        {
            if(this is IWidthStyle widthSource && target is IWidthStyle widthTarget)
                widthTarget.Width = widthSource.Width;
            if (this is IColorStyle colorSource && target is IColorStyle colorTarget)
                colorTarget.Color = colorSource.Color;
        }

        public virtual List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = new List<UIComponent>
            {
                AddColorProperty(parent),
                AddWidthProperty(parent, onHover, onLeave),
            };

            return components;
        }
        protected ColorPropertyPanel AddColorProperty(UIComponent parent)
        {
            var colorProperty = parent.AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = Localize.LineEditor_Color;
            colorProperty.Init();
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color = color;
            return colorProperty;
        }
        protected FloatPropertyPanel AddWidthProperty(UIComponent parent, Action onHover, Action onLeave)
        {
            var widthProperty = parent.AddUIComponent<FloatPropertyPanel>();
            widthProperty.Text = Localize.LineEditor_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.01f;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width = value;
            AddOnHoverLeave(widthProperty, onHover, onLeave);

            return widthProperty;
        }
        protected static void AddOnHoverLeave<T>(FieldPropertyPanel<T> fieldPanel, Action onHover, Action onLeave)
        {
            if (onHover != null)
                fieldPanel.OnHover += onHover;
            if (onLeave != null)
                fieldPanel.OnLeave += onLeave;
        }


        public enum StyleType
        {
            ItemMask = 0xFF,
            GroupMask = ~ItemMask,

            RegularLine = 0x100,

            [Description(nameof(Localize.LineStyle_Solid))]
            LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            LineSolidAndDashed,


            StopLine = 0x200,

            [Description(nameof(Localize.LineStyle_Stop))]
            StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            StopLineDoubleDashed,


            Filler = 0x400,

            [Description(nameof(Localize.FillerStyle_Stripe))]
            FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            FillerSolid,
        }
    }

    public class MarkupStyleDash
    {
        public Vector3 Position { get; set; }
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color Color { get; set; }

        public MarkupStyleDash(Vector3 position, float angle, float length, float width, Color color)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
        }
    }
    public class StyleTemplate : IToXml
    {
        public static string XmlName { get; } = "T";

        string _name;
        Style _style;

        public string Name
        {
            get => _name;
            set
            {
                if (OnNameChanged?.Invoke(this, value) == true)
                {
                    _name = value;
                    TemplateChanged();
                }
            }
        }
        public Style Style
        {
            get => _style;
            set
            {
                OnStyleChanged?.Invoke(this, value);
                _style = value;
                TemplateChanged();
            }
        }
        public Action OnTemplateChanged { private get; set; }
        public Action<StyleTemplate, Style> OnStyleChanged { private get; set; }
        public Func<StyleTemplate, string, bool> OnNameChanged { private get; set; }

        public string XmlSection => XmlName;

        public StyleTemplate(string name, Style style)
        {
            _name = name;
            _style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }
        private void TemplateChanged() => OnTemplateChanged?.Invoke();

        public override string ToString() => Name;
        public string ToStringWithShort() => $"{Style.GetShortName(Style.Type)}-{Name}";

        public static bool FromXml(XElement config, out StyleTemplate template)
        {
            var name = config.GetAttrValue<string>("N");
            if (!string.IsNullOrEmpty(name) && config.Element(Style.XmlName) is XElement styleConfig && Style.FromXml(styleConfig, out Style style))
            {
                template = new StyleTemplate(name, style);
                return true;
            }
            else
            {
                template = default;
                return false;
            }
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlName,
                new XAttribute("N", Name),
                Style.ToXml()
                );
            return config;
        }
    }
}
