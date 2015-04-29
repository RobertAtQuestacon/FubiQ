using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Fubi_WPF_GUI.UpDownCtrls
{
    public enum DecimalSeparatorType
    {
        SystemDefined,
        Point,
        Comma
    }
    public enum NegativeSignType
    {
        SystemDefined,
        Minus
    }
    public enum NegativeSignSide
    {
        SystemDefined,
        Prefix,
        Suffix
    }
    internal interface IFrameTxtBoxCtrl
    {
        TextBox TextBox { get; }
    }
    static class Coercer
    {
        public static void Initialize<T>()
        {
            var borderThicknessMetaData = new FrameworkPropertyMetadata
            {
	            CoerceValueCallback = CoerceBorderThickness
            };
	        Control.BorderThicknessProperty.OverrideMetadata(typeof(T), borderThicknessMetaData);

            // For Background, do not do in XAML part something like:
            // Background="{Binding Background, ElementName=Root}" in TextBoxCtrl settings.
            // Reason: although this will indeed set the Background values as expected, problems arise when user
            // of control does not explicitly not set a value.
            // In this case, Background of TextBoxCtrl get defaulted to values in UserControl, which is null
            // and not what we want.
            // We want to keep the default values of a standard TextBox, which may differ according to themes.
            // Have to treat similarly as with BorderThickness...

            var backgroundMetaData = new FrameworkPropertyMetadata {CoerceValueCallback = CoerceBackground};
	        Control.BackgroundProperty.OverrideMetadata(typeof(T), backgroundMetaData);

            // ... Same for BorderBrush
            var borderBrushMetaData = new FrameworkPropertyMetadata {CoerceValueCallback = CoerceBorderBrush};
	        Control.BorderBrushProperty.OverrideMetadata(typeof(T), borderBrushMetaData);
        }
        private delegate void FuncCoerce(IFrameTxtBoxCtrl frameTxtBox, object value);

        private static void CommonCoerce(DependencyObject d, object value, FuncCoerce funco)
        {
            var frameTxtBox = d as IFrameTxtBoxCtrl;

            if (frameTxtBox != null)
                funco(frameTxtBox, value);
        }
        private static void FuncCoerceBorderThickness(IFrameTxtBoxCtrl frameTxtBox, object value)
        {
            if (value is Thickness)
                frameTxtBox.TextBox.BorderThickness = (Thickness)value;
        }
        private static void FuncCoerceBackground(IFrameTxtBoxCtrl frameTxtBox, object value)
        {
	        var brush = value as Brush;
	        if (brush != null)
                frameTxtBox.TextBox.Background = brush;
        }

	    private static void FuncCoerceBorderBrush(IFrameTxtBoxCtrl frameTxtBox, object value)
	    {
		    var brush = value as Brush;
		    if (brush != null)
                frameTxtBox.TextBox.BorderBrush = brush;
	    }

	    public static object CoerceBorderThickness(DependencyObject d, object value)
        {
            CommonCoerce(d, value, FuncCoerceBorderThickness);
            return new Thickness(0.0);
        }
        public static object CoerceBackground(DependencyObject d, object value)
        {
            CommonCoerce(d, value, FuncCoerceBackground);
            return value;
        }
        public static object CoerceBorderBrush(DependencyObject d, object value)
        {
            CommonCoerce(d, value, FuncCoerceBorderBrush);
            return value;
        }
    }
    static class SystemNumberInfo
    {
        static private readonly NumberFormatInfo Nfi;

        static SystemNumberInfo()
        {
            var ci = CultureInfo.CurrentCulture;
            Nfi = ci.NumberFormat;
        }
        public static string DecimalSeparator
        {
            get { return Nfi.NumberDecimalSeparator; }
        }
        public static string NegativeSign
        {
            get { return Nfi.NegativeSign; }
        }
        public static bool IsNegativePrefix
        {
            // for values, see: http://msdn.microsoft.com/en-us/library/system.globalization.numberformatinfo.numbernegativepattern.aspx
            // Assume if negative number format is (xxx), number is prefixed.
            get
            {
                return Nfi.NumberNegativePattern < 3;
            }
        }
    }
    public class ThicknessToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (!(value is Thickness) || !(parameter is string))
                throw new ArgumentException();

            bool incrRightThickness;
            bool.TryParse((string)parameter, out incrRightThickness);
            var thickness = (Thickness)value;
            return new Thickness(thickness.Left + 1.0, thickness.Top + 1.0, incrRightThickness ? thickness.Right + 18.0 : 18.0, thickness.Bottom);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Not Implemented.");
        }
    }

    public enum ButtonArrowType : byte
    {
        Down,
        Up,
        Left,
        Right
    }

    [ValueConversion(typeof(IsCornerCtrlCorner), typeof(CornerRadius))]
    public class IsCornerCtrlCornerToRadiusConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Must be a string that can be converted into an int, either directly as a decimal or as an hexadecimal
        /// prefixed with either 0x or 0X. The first 8 bits will give the CornerRadius rounding next to edge of control. The following bits will
        /// give rounding of a corner not adjoining an edge. Example: 0x305: inner rounding: 0x3, outer rounding: 0x5. Inner rounding of a 
        /// corner not adjoining an edge currently not used and set to 0.</param>
        /// <param name="culture"></param>
        /// <returns></returns>

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                 throw new ArgumentNullException();

            if (!(value is IsCornerCtrlCorner))
                throw new ArgumentException();

            var str = (string)parameter;
            var ccc = (IsCornerCtrlCorner)value; 
            int rounding;

            if (!int.TryParse(str, out rounding))
            {
                if (!(str[0] == '0' && (str[1] == 'x' || str[1] == 'X')))
                    throw new ArgumentException();

                if (!Int32.TryParse(str.Substring(2), NumberStyles.HexNumber, null, out rounding))
                    throw new ArgumentException();
            }
            var notEdgeRounding = rounding >> 8;
            var edgeRounding = rounding & 0x000000FF;

            return new CornerRadius(ccc.TopLeft? edgeRounding : notEdgeRounding, ccc.TopRight? edgeRounding : notEdgeRounding,
                                    ccc.BottomRight? edgeRounding : notEdgeRounding, ccc.BottomLeft? edgeRounding : notEdgeRounding);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Not Implemented.");
        }
    }

    public class IsCornerCtrlCornerConverter : TypeConverter
    {
	    public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            return sourceType == Type.GetType("System.String"); 
        }
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == Type.GetType("System.String"); 
        }
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
        {
            if (source == null)
                throw new ArgumentNullException();

            var strArr = ((String)source).Split(',');

            if (strArr.Count() != 4)
                throw new ArgumentException();

            var cornerstates = new bool[4];

            for (var i = 0; i < strArr.Count(); i++)
            {
                if (!bool.TryParse(strArr[i], out cornerstates[i]))
                    throw new ArgumentException();
            }
            return new IsCornerCtrlCorner(cornerstates[0], cornerstates[1], cornerstates[2], cornerstates[3]);
        }
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            if (value == null)
                throw new ArgumentNullException();
            if (!(value is IsCornerCtrlCorner))
                throw new ArgumentException();

            var ccc = (IsCornerCtrlCorner)(value);

            return ccc.TopLeft.ToString() + "," + ccc.TopRight.ToString() + "," + ccc.BottomRight.ToString() + "," + ccc.BottomLeft.ToString();
        }
    }

    /// <summary>
    /// IsCornerCtrlCorner is used to indicate which corners of the arrow button are also on the corner of the container control
    /// in which it is inserted. If for instance the arrow button is placed on right hand side as with a combo box, then both 
    /// right hand sides corners of IsCornerCtrlCorner will be set to true, while both left hand sides will be set to false.
    /// Order is same as with Border.CornerRadius: topleft, topright, bottomright, bottomleft.
    /// Reason for this is because with some themes (example: Aero), button has slightly rounded corners when these are on 
    /// edge of control.
    /// </summary>
    [TypeConverter(typeof(IsCornerCtrlCornerConverter))]
    public struct IsCornerCtrlCorner : IEquatable<IsCornerCtrlCorner>
    {
        private bool m_topLeft;
	    private bool m_topRight;
	    private bool m_bottomRight;
	    private bool m_bottomLeft;

	    public IsCornerCtrlCorner(bool uniformCtrlCorner)
        {
           m_topLeft = m_topRight = m_bottomRight = m_bottomLeft = uniformCtrlCorner;
        }
        public IsCornerCtrlCorner(bool topLeft, bool topRight, bool bottomRight, bool bottomLeft)
        {
            m_topLeft = topLeft;
            m_topRight = topRight;
            m_bottomRight = bottomRight;
            m_bottomLeft = bottomLeft;
        }
        public bool BottomLeft {get { return m_bottomLeft; } set { m_bottomLeft = value; }}
        public bool BottomRight {get { return m_bottomRight; } set { m_bottomRight = value; }}
        public bool TopLeft {get { return m_topLeft; } set { m_topLeft = value; }}
        public bool TopRight {get { return m_topRight; } set { m_topRight = value; }}

        public static bool operator !=(IsCornerCtrlCorner ccc1, IsCornerCtrlCorner ccc2)
        {
            return ccc1.m_topLeft != ccc2.m_topLeft || ccc1.m_topRight != ccc2.m_topRight ||
                    ccc1.m_bottomRight != ccc2.m_bottomRight || ccc1.m_bottomLeft != ccc2.m_topLeft;
        }
        public static bool operator ==(IsCornerCtrlCorner ccc1, IsCornerCtrlCorner ccc2)
        {
            return ccc1.m_topLeft == ccc2.m_topLeft && ccc1.m_topRight == ccc2.m_topRight &&
                ccc1.m_bottomRight == ccc2.m_bottomRight && ccc1.m_bottomLeft == ccc2.m_topLeft;
        }
        public bool Equals(IsCornerCtrlCorner cornerCtrlCorner)
        {
            return this == cornerCtrlCorner;
        }
        public override bool Equals(object obj)
        {
	        if (obj is IsCornerCtrlCorner)
                return this == (IsCornerCtrlCorner)obj;
	        return false;
        }

	    public override int GetHashCode()
        {
            return (m_topLeft? 0x00001000 : 0x00000000) | (m_topRight? 0x00000100 : 0x00000000) |
                (m_bottomRight? 0x00000010 : 0x00000000) | (m_bottomLeft? 0x00000001 : 0x00000000);
        }
        public override string ToString()
        {
            return m_topLeft.ToString() + "," + m_topRight.ToString() + "," + m_bottomRight.ToString() + "," + m_bottomLeft.ToString();
        }
    }

    public class ArrowRepeatButton : RepeatButton
    {
        private static readonly DependencyProperty ButtonArrowTypeProperty =
            DependencyProperty.Register("ButtonArrowType", typeof(ButtonArrowType), typeof(ArrowRepeatButton), new FrameworkPropertyMetadata(ButtonArrowType.Down));
        private static readonly DependencyProperty IsCornerCtrlCornerProperty =
            DependencyProperty.Register("IsCornerCtrlCorner", typeof(IsCornerCtrlCorner), typeof(ArrowRepeatButton), new FrameworkPropertyMetadata(new IsCornerCtrlCorner(false, true, true, false)));

        static ArrowRepeatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ArrowRepeatButton), new FrameworkPropertyMetadata(typeof(ArrowRepeatButton)));
        }
        public ButtonArrowType ButtonArrowType
        {
            get { return (ButtonArrowType)GetValue(ButtonArrowTypeProperty); }
            set { SetValue(ButtonArrowTypeProperty, value); }
        }
        public IsCornerCtrlCorner IsCornerCtrlCorner
        {
            get { return (IsCornerCtrlCorner)GetValue(IsCornerCtrlCornerProperty); }
            set { SetValue(IsCornerCtrlCornerProperty, value); }
        }
    }
}
