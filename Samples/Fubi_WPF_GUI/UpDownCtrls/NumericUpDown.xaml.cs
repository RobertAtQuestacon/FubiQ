using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Fubi_WPF_GUI.UpDownCtrls
{
    /// <summary>
    /// Interaction logic for NumericUpDown.xaml
    /// </summary>
    public partial class NumericUpDown : IFrameTxtBoxCtrl
    {
        static NumericUpDown()
        {
            Coercer.Initialize<NumericUpDown>();

            ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NumericUpDown));
        }
        public static RoutedEvent ValueChangedEvent;
        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }
        protected virtual void OnValueChanged()
        {
            var args = new RoutedEventArgs {RoutedEvent = ValueChangedEvent};
	        RaiseEvent(args);
        }

        private static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(NumericUpDown), new PropertyMetadata(100.0));

        private static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0));

        private static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(NumericUpDown), new PropertyMetadata(0.0, ValueChangedCallback));
        
        private static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(double), typeof(NumericUpDown), new PropertyMetadata(1.0));

        private static readonly DependencyProperty DecimalPlacesProperty =
            DependencyProperty.Register("DecimalPlaces", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

        private static readonly DependencyProperty DecimalSeparatorTypeProperty =
            DependencyProperty.Register("DecimalSeparatorType", typeof(DecimalSeparatorType), typeof(NumericUpDown), new PropertyMetadata(DecimalSeparatorType.SystemDefined));

        private static readonly DependencyProperty NegativeSignTypeProperty =
            DependencyProperty.Register("NegativeSignType", typeof(NegativeSignType), typeof(NumericUpDown), new PropertyMetadata(NegativeSignType.SystemDefined));

        private static readonly DependencyProperty NegativeSignSideProperty =
            DependencyProperty.Register("NegativeSignSide", typeof(NegativeSignSide), typeof(NumericUpDown), new PropertyMetadata(NegativeSignSide.SystemDefined));

        private static readonly DependencyProperty NegativeTextBrushProperty =
            DependencyProperty.Register("NegativeTextBrush", typeof(Brush), typeof(NumericUpDown));

        private static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(NumericUpDown), new PropertyMetadata(TextAlignment.Right, TextAlignmentChangedCallback));

        private static readonly DependencyProperty OutOfRangeTextBrushProperty =
            DependencyProperty.Register("OutOfRangeTextBrush", typeof(Brush), typeof(NumericUpDown));

        private static readonly DependencyProperty UseMinusOneAsInvalidProperty =
            DependencyProperty.Register("UseMinusOneAsInvalid", typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));
  
        private static void ValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
           var numUpDown = d as NumericUpDown;

            if (numUpDown != null && e.NewValue is double)
            {
                numUpDown.FormatTextBox((double)e.NewValue);
                numUpDown.OnValueChanged();
            }
        }
        private static void TextAlignmentChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numUpDown = d as NumericUpDown;

            if (numUpDown != null && e.NewValue is TextAlignment)
            {
                numUpDown.TextBoxCtrl.TextAlignment = (TextAlignment)e.NewValue;
            }
        }
        struct TempCutCopyInfo
        {
            public int Pos;
            public double Value;
            public string CutStr;
        }
        private TempCutCopyInfo m_tempCcInf;

        public NumericUpDown()
        {
            InitializeComponent();
        }
        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
	        if (Value < Minimum)
	        {
				Value = UseMinusOneAsInvalid ? (Minimum-1) : Minimum;
	        }
            else if (Value > Maximum)
                Value = Maximum;
            
            FormatTextBox(Value);
        }
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public double Step
        {
            get { return (double)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }
        public int DecimalPlaces
        {
            get { return (int)GetValue(DecimalPlacesProperty); }
            set { SetValue(DecimalPlacesProperty, value); }
        }
        public DecimalSeparatorType DecimalSeparatorType
        {
            get { return (DecimalSeparatorType)GetValue(DecimalSeparatorTypeProperty); }
            set { SetValue(DecimalSeparatorTypeProperty, value); }
        }
        public NegativeSignType NegativeSignType
        {
            get { return (NegativeSignType)GetValue(NegativeSignTypeProperty); }
            set { SetValue(NegativeSignTypeProperty, value); }
        }
        public NegativeSignSide NegativeSignSide
        {
            get { return (NegativeSignSide)GetValue(NegativeSignSideProperty); }
            set { SetValue(NegativeSignSideProperty, value); }
        }
        public Brush NegativeTextBrush
        {
            get { return (Brush)GetValue(NegativeTextBrushProperty); }
            set { SetValue(NegativeTextBrushProperty, value); }
        }
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }
        public Brush OutOfRangeTextBrush
        {
            get { return (Brush)GetValue(OutOfRangeTextBrushProperty); }
            set { SetValue(OutOfRangeTextBrushProperty, value); }
        }
        public bool UseMinusOneAsInvalid
        {
            get { return (bool)GetValue(UseMinusOneAsInvalidProperty); }
            set { SetValue(UseMinusOneAsInvalidProperty, value); }
        }
        TextBox IFrameTxtBoxCtrl.TextBox 
        {
            get { return TextBoxCtrl; } 
        }
        private Brush GetTextBrush(double dec)
        {
            return IsEnabled ? (dec < 0.0 && NegativeTextBrush != null) ? NegativeTextBrush : Foreground : SystemColors.GrayTextBrush;
        }
        private void FormatTextBox(double dec)
        {
            TextBoxCtrl.Foreground = GetTextBrush(dec);
            TextBoxCtrl.Text = dec.ToString("N", GetNumberFormat());
        }
        private void UpdateInput()
        {
            double currentValue;
            var txt = TextBoxCtrl.Text;
            if (ValidateInput(ref txt, out currentValue))
                Value = currentValue;

            FormatTextBox(Value);
        }

        private delegate void OperateAction();

        private void UpIncr()
        {
            if (Value < Minimum)
                Value = Minimum;
            else if (Value + Step <= Maximum)
                Value += Step;
        }
        private void DownIncr()
        {
            if (Value - Step >= Minimum)
                Value -= Step;
            else if (UseMinusOneAsInvalid)
                Value = Minimum - 1.0;
        }
        private void OnOperateAction(OperateAction opAct)
        {
            UpdateInput();
            opAct();                      
            TextBoxCtrl.Focus();
            TextBoxCtrl.SelectAll();
        }
        private void OnUpIncr()
        {
            OnOperateAction(UpIncr);
        }
        private void OnDownIncr()
        {
            OnOperateAction(DownIncr);
        }
        private void UpDown_UpClick(object sender, RoutedEventArgs e)
        {
            OnUpIncr();
        }
        private void UpDown_DownClick(object sender, RoutedEventArgs e)
        {
            OnDownIncr();
        }
        private void UpdateTextBoxTxt(string txt, int curPos, ref double currentValue)
        {
            if (OutOfRangeTextBrush != null && (currentValue < Minimum || currentValue > Maximum))
                TextBoxCtrl.Foreground = OutOfRangeTextBrush; 
            else
                TextBoxCtrl.Foreground = (txt.Count(chr => chr == GetNegativeSign()[0]) == 1 && NegativeTextBrush != null) ?
                                                                                                NegativeTextBrush : Foreground;
            TextBoxCtrl.Text = txt;
            TextBoxCtrl.SelectionStart = curPos;
            OnValueChanged();
        }
        private void TextBoxCtrl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;

            if (e.Key == Key.Space) // Want to prevent entering spaces. Amazingly, TextBoxCtrl_PreviewTextInput not called, even though
                                    // TextBoxCtrl_PreviewTextInput IS called when enter return key (and e.Text = '\r') ??!!!  
                e.Handled = true;
            else if (e.Key == Key.Left)
            {
                // Prevent overlapping over an existing negative sign.
                if (IsNegativePrefix() && !string.IsNullOrEmpty(TextBoxCtrl.Text)
                                       && TextBoxCtrl.Text[0] == GetNegativeSign()[0])
                    e.Handled = (TextBoxCtrl.SelectionStart <= 1);
            }
            else if (e.Key == Key.Right)
            {
                if (!IsNegativePrefix() && !string.IsNullOrEmpty(TextBoxCtrl.Text)
                       && TextBoxCtrl.Text[TextBoxCtrl.Text.Length - 1] == GetNegativeSign()[0])
                    e.Handled = (TextBoxCtrl.SelectionStart == TextBoxCtrl.Text.Length - 1);
            }
            else if (e.Key == Key.Up)
            {
                OnUpIncr();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                OnDownIncr();
                e.Handled = true;
            }
            else if (e.Key == Key.Home || e.Key == Key.PageUp)
            {
                if (IsNegativePrefix() && !string.IsNullOrEmpty(TextBoxCtrl.Text)
                                                        && TextBoxCtrl.Text[0] == GetNegativeSign()[0])
                {
                    TextBoxCtrl.SelectionStart = 1;
                    TextBoxCtrl.SelectionLength = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.End || e.Key == Key.PageDown)
            {
                if (!IsNegativePrefix() && !string.IsNullOrEmpty(TextBoxCtrl.Text)
                                        && TextBoxCtrl.Text[TextBoxCtrl.Text.Length - 1] == GetNegativeSign()[0])
                {
                    TextBoxCtrl.SelectionStart = TextBoxCtrl.Text.Length - 1;
                    TextBoxCtrl.SelectionLength = 0;
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                var curPos = TextBoxCtrl.SelectionStart;
                var txt = TextBoxCtrl.Text;
                var updateTxt = true;

                if (TextBoxCtrl.SelectionLength > 0)
                    txt = txt.Remove(curPos, TextBoxCtrl.SelectionLength);
                else if (e.Key == Key.Delete)
                {
                    if (curPos < txt.Length)
                        txt = txt.Remove(curPos, 1);
                    else
                        updateTxt = false;
                }
                else if (curPos > 0)
                    txt = txt.Remove(--curPos, 1);
                else
                    updateTxt = false;

                double currentValue;
                if (updateTxt && ValidateInput(ref txt, out currentValue, false))
                    UpdateTextBoxTxt(txt, curPos, ref currentValue);

                e.Handled = true;
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                UpdateInput();
            }

        }
        public string GetDecimalSeparator()
        {
            switch (DecimalSeparatorType)
            {
                case DecimalSeparatorType.Point:
                    return ".";
                case DecimalSeparatorType.Comma:
                    return ",";
	            default:
                    return SystemNumberInfo.DecimalSeparator;
            }
        }
        public string GetNegativeSign()
        {
            switch (NegativeSignType)
            {
                case NegativeSignType.Minus:
                    return "-";
	            default:
                    return SystemNumberInfo.NegativeSign;
            }
        }
        public bool IsNegativePrefix()
        {
            switch (NegativeSignSide)
            {
                case NegativeSignSide.Prefix:
                    return true;
                case NegativeSignSide.Suffix:
                    return false;
	            default:
                    return SystemNumberInfo.IsNegativePrefix;
            }
        }
        IFormatProvider GetNumberFormat()
        {
            var info = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            var nfi = info.NumberFormat;
            nfi.NumberDecimalSeparator = GetDecimalSeparator();
            nfi.NumberDecimalDigits = (DecimalPlaces >= 0) ? DecimalPlaces : 0; // Only works for output, not input as we require here...
            nfi.NegativeSign = GetNegativeSign();
            nfi.CurrencyDecimalDigits = (DecimalPlaces >= 0) ? DecimalPlaces : 0;
            nfi.NumberGroupSeparator = "";

            if (IsNegativePrefix() && nfi.NumberNegativePattern >= 3)
                nfi.NumberNegativePattern = 1;

            if (!IsNegativePrefix() && nfi.NumberNegativePattern < 3)
                nfi.NumberNegativePattern = 3;

            return info;
        }
        private bool GetCurrentValue(string txt, out double currentValue)
        {
            // We need a bit of an extension. We require value such as "", "-", "." or "12." to be valid 
            // (with negative sign set to '-', and double separator set to '.' of course)
            if (txt != null)
            {
                // if double places > 0 and only one double separator typed and it is after all digits... 
                // this makes an entry such as 12. valid.
                if (DecimalPlaces > 0 && txt.Count(chr => chr == GetDecimalSeparator()[0]) == 1
                  && ((txt.Length > 0 && txt[txt.Length - 1] == GetDecimalSeparator()[0])
                 || (txt.Length > 1 && txt[txt.Length - 1] == GetNegativeSign()[0] && txt[txt.Length - 2] == GetDecimalSeparator()[0])))
                    txt = txt.Replace(GetDecimalSeparator(), "");

                if (txt == "" || txt == GetNegativeSign())
                {
                    currentValue = 0.0;
                    return true;
                }
                if (txt.Length > 0)
                    return double.TryParse(txt, ((DecimalPlaces > 0) ? NumberStyles.AllowDecimalPoint : 0)
                        | (IsNegativePrefix() ? NumberStyles.AllowLeadingSign : NumberStyles.AllowTrailingSign), GetNumberFormat(), out currentValue);
            }
            currentValue = 0.0;
            return false;
        }
        private bool ValidateInput(ref string txt, out double currentValue, bool rangeCheck = true)
        {
            if (DecimalPlaces > 0) // If Txt has more digits than double places, trim out excess digits, otherwise get funny results.
            {
	            int idx = txt.IndexOf(GetDecimalSeparator(), StringComparison.Ordinal);

                if (idx != -1)
                {
                    int negSymbIdx = txt.IndexOf(GetNegativeSign(), ++idx, StringComparison.Ordinal);
                    int length = (negSymbIdx != -1)? negSymbIdx - idx : txt.Length - idx;

                    if (length > DecimalPlaces)
                        txt = txt.Remove(idx + DecimalPlaces, length - DecimalPlaces);
                }
            }
            var isValid = GetCurrentValue(txt, out currentValue);

            if (isValid && rangeCheck)
            {
                if (currentValue > Maximum)
                    currentValue = Maximum;
                if (currentValue < Minimum)
                    currentValue = UseMinusOneAsInvalid ? (Minimum - 1.0) : Minimum;
            }
            return isValid;
        }
        private void TextBoxCtrl_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 0)
                return;

            var curPos = TextBoxCtrl.SelectionStart;
            string txt;

            if (e.Text == GetNegativeSign() && (Minimum < 0 || UseMinusOneAsInvalid && Minimum-1.0 < 0))
            {
                txt = TextBoxCtrl.Text;
                if (string.IsNullOrEmpty(txt))
                {
                    txt = GetNegativeSign();

                    if (IsNegativePrefix())
                        curPos++;
                }
                else
                {
                    if (IsNegativePrefix())
                    {
                        if ((txt[0] == GetNegativeSign()[0]))
                        {
                            txt = txt.Remove(0, 1);
                            if (curPos > 0) curPos--;
                        }
                        else
                        {
                            txt = GetNegativeSign() + txt;
                            curPos++;
                        }
                    }
                    else
                        txt = (txt[txt.Length - 1] == GetNegativeSign()[0]) ? txt.Remove(txt.Length - 1, 1) : txt + GetNegativeSign();
                }
            }
            else if (e.Text != GetNegativeSign())
            {
                txt = TextBoxCtrl.Text.Remove(curPos, TextBoxCtrl.SelectionLength);

                if (Keyboard.GetKeyStates(Key.Insert) == KeyStates.Toggled && curPos < txt.Length)
                    txt = txt.Remove(curPos, 1);

                txt = txt.Insert(curPos, e.Text);
                curPos++;
            }
            else
                txt = TextBoxCtrl.Text;

            double currentValue;
            if (ValidateInput(ref txt, out currentValue, false))
            {
                // Remove any leading '0'. 
                int leadZeroIdx = 0, startPos = 0;

                if (txt.Length > 0 && txt[startPos] == GetNegativeSign()[0])
                    leadZeroIdx = startPos = 1;

                while (leadZeroIdx < txt.Length && txt[leadZeroIdx] == '0')
                    leadZeroIdx++;
 
                // Keep just 1 leading '0' unless 1st non zero is a digit.
                if (leadZeroIdx != startPos && !(leadZeroIdx < txt.Length && char.IsDigit(txt[leadZeroIdx])))
                    --leadZeroIdx;

                txt = txt.Remove(startPos, leadZeroIdx - startPos);

                if (e.Text == "0" && curPos == startPos + 1)
                    curPos = startPos;

                // This is for case of a suffixed negative sign and double places > 0. If type a number at end of string, this
                // is removed because it has moved beyond double places allowed. However, in this case do not want to move caret, 
                // otherwise it moves over negative sign.   
                if (e.Text != GetNegativeSign() && curPos - 1 < txt.Length && !IsNegativePrefix() &&
                                                            txt[curPos - 1] == GetNegativeSign()[0] && curPos > 0)
                    curPos--;

                UpdateTextBoxTxt(txt, curPos, ref currentValue);
            }
            e.Handled = true;
        }
        private void TextBoxCtrl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                OnUpIncr();
            else
                OnDownIncr();
        }
        private void TextBoxCtrl_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            UpdateInput();
        }
        private void Root_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBoxCtrl.Foreground = GetTextBrush(Value);
        }
        private void InitTempInf(bool canExecute, int selStart, double currentValue, string currentSel)
        {
            if (canExecute)
            {
                m_tempCcInf.Pos = selStart;
                m_tempCcInf.Value = currentValue;
                m_tempCcInf.CutStr = currentSel;
            }
        }     
        private void Command_Cut_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var currentValue = 0.0;
            var newTxt = TextBoxCtrl.Text.Remove(TextBoxCtrl.SelectionStart, TextBoxCtrl.SelectionLength);
            e.CanExecute = TextBoxCtrl.SelectionLength > 0 &&
               ValidateInput(ref newTxt, out currentValue);

            InitTempInf(e.CanExecute, TextBoxCtrl.SelectionStart, currentValue, 
                                            TextBoxCtrl.Text.Substring(TextBoxCtrl.SelectionStart, TextBoxCtrl.SelectionLength));
            e.Handled = true;
        }
        private void Command_Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
	        var clipTxt = (string)Clipboard.GetData("Text");

            if (string.IsNullOrEmpty(clipTxt))
                e.CanExecute = false;
            else
            {
                var curPos = TextBoxCtrl.SelectionStart;
                var txt = TextBoxCtrl.Text.Remove(curPos, TextBoxCtrl.SelectionLength);
                txt = txt.Insert(curPos, clipTxt);
	            double currentValue;
	            e.CanExecute = ValidateInput(ref txt, out currentValue);
                InitTempInf(e.CanExecute, TextBoxCtrl.SelectionStart, currentValue, "");
            }
            e.Handled = true;
        }
        private void CommandBinding_CutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Value = m_tempCcInf.Value;
            TextBoxCtrl.SelectionStart = m_tempCcInf.Pos;
            Clipboard.SetData("Text", m_tempCcInf.CutStr);
            e.Handled = true;
        }
        private void CommandBinding_PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Value = m_tempCcInf.Value;
            TextBoxCtrl.SelectionStart = m_tempCcInf.Pos;
        }
        private void TextBoxCtrl_PreviewDragOver(object sender, DragEventArgs e)
        {
           var ctrl = sender as Control;

            if (ctrl != null)
            {
                var dragTxt = (string)e.Data.GetData("Text");
                var pt = e.GetPosition(TextBoxCtrl);

	            TextBoxCtrl.Focus();
                                
                var charPosClosest = TextBoxCtrl.GetCharacterIndexFromPoint(pt, true); // With SnapToText = false, always get 1 returned 

                if (charPosClosest == TextBoxCtrl.Text.Length - 1)
                {
                    // According to MSDN documentation, GetRectFromCharacterIndex always returns the zero-width rectangle preceeding 
                    // the character.
                    // Documentation says nothing about one past the last character. This I just guessed. Logically it should work,
                    // and you should be able to get this information, but documentation says nothing about this.
                    var rc = TextBoxCtrl.GetRectFromCharacterIndex(charPosClosest+1);

                    if (rc.Right < pt.X)
                        charPosClosest++;
                }
                TextBoxCtrl.SelectionStart = charPosClosest;

                TextBoxCtrl.SelectionLength = 0;
                double currentValue;

                var curPos = TextBoxCtrl.SelectionStart;          
                var txt = TextBoxCtrl.Text.Insert(curPos, dragTxt);
                e.Effects = ValidateInput(ref txt, out currentValue) ? DragDropEffects.Move : DragDropEffects.None;
            }
            e.Handled = true;
        }
        private void TextBoxCtrl_PreviewDrop(object sender, DragEventArgs e)
        {
            var dragTxt = (string)e.Data.GetData("Text");
            var curPos = TextBoxCtrl.SelectionStart;
            var txt = TextBoxCtrl.Text.Insert(curPos, dragTxt);
            double currentValue;

            if (ValidateInput(ref txt, out currentValue))
            {
                e.Effects = DragDropEffects.Move;
                Value = currentValue;
                TextBoxCtrl.SelectionStart = curPos;
                TextBoxCtrl.SelectionLength = dragTxt.Length;
            }
            e.Handled = true;
        }
        private void TextBoxCtrl_PreviewQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Very reluctantly, preventing this control from initiating any drag drop operations.
            // There are cases when this should be prevented anyway or only copy allowed (not move): when text entry is no longer a valid – see
            // commented code below.
            // Otherwise, the reason I am preventing drag and drop are because: 
            //      (i)	It does not seem possible to receive a notification that a drop has taken place. e.Action is never set to DragAction.Drop,
            //      and I have not found a workaround. GiveFeedback does not seem to notify a drop has actually taken place either. We need to
            //      receive this notification, so underlying double Value can be correctly changed.
            //      (ii) It does not seem possible to indicate if the drop target is the same as the source. This is very pertinent. For instance,
            //      user enters a number “12.34”, then selects “.3” and drags that just after the “1”. User would expect to get “1.324”. To do this,
            //      We would need to know that the source is same as destination, as “.3” needs to be removed, before being reinserted. That is, 
            //      you need to insert it in “124” and not “12.34”, otherwise get “1.32.34”. Works fine with normal textboxes, but here doing number
            //      validation, and “1.32.34” is obviously an invalid number.
            // Otherwise, cut/copy/paste should work fine. Only drag/drop not functional.
            e.Action = DragAction.Cancel;
            e.Handled = true;
            /*string Txt = TextBoxCtrl.Text.Remove(TextBoxCtrl.SelectionStart, TextBoxCtrl.SelectionLength);
            double CurrentValue;

            if (!ValidateInput(Txt, out CurrentValue))
            {
                // Would be nice if we could have effects set to copy only (not move), but this does not seem possible.
                // This is because e.Effects: not available in QueryContinueDrag,and read-only in GiveFeedBack.
                e.Action = DragAction.Cancel;
                e.Handled = true;
            }*/
        }
    }
}
