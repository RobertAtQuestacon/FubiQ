using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace Fubi_WPF_GUI
{
	/// <summary>
	/// Interaction logic for PlaybackSlider.xaml
	/// </summary>
	public partial class PlaybackSlider
	{
		public int Minimum
		{
			get { return (int)GetValue(MinimumProperty); }
			set { SetValue(MinimumProperty, value); }
		}
		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register("Minimum", typeof(int), typeof(PlaybackSlider), new UIPropertyMetadata(0));
		public int StartValue
		{
			get { return (int)GetValue(StartValueProperty); }
			set { SetValue(StartValueProperty, value); }
		}
		public static readonly DependencyProperty StartValueProperty =
			DependencyProperty.Register("StartValue", typeof(int), typeof(PlaybackSlider), new UIPropertyMetadata(0));
		public int EndValue
		{
			get { return (int)GetValue(EndValueProperty); }
			set { SetValue(EndValueProperty, value); }
		}
		public static readonly DependencyProperty EndValueProperty =
			DependencyProperty.Register("EndValue", typeof(int), typeof(PlaybackSlider), new UIPropertyMetadata(1));
		public int Value
		{
			get { return (int)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register("Value", typeof(int), typeof(PlaybackSlider), new UIPropertyMetadata(0));
		public int Maximum
		{
			get { return (int)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}
		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register("Maximum", typeof(int), typeof(PlaybackSlider), new UIPropertyMetadata(1));
		public double TickFrequency
		{
			get { return (double)GetValue(TickFrequencyProperty); }
			set { SetValue(TickFrequencyProperty, value); }
		}
		public static readonly DependencyProperty TickFrequencyProperty =
			DependencyProperty.Register("TickFrequency", typeof(double), typeof(PlaybackSlider), new UIPropertyMetadata(5d));


		public event EventHandler ValueChanged, ThumbDragStart, ThumbDragDelta, ThumbDragEnd, StartValueChanged, EndValueChanged;


		private bool m_isDragging;

		public PlaybackSlider()
		{
			InitializeComponent();
		}
		private void OnLoad(object sender, RoutedEventArgs e)
		{
		}
		private void leftSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			rightSlider.Value = Math.Max(rightSlider.Value, leftSlider.Value);
			middleSlider.Value = Math.Max(middleSlider.Value, leftSlider.Value);
			if (StartValueChanged != null)
				StartValueChanged(this, e);
		}
		private void rightSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			leftSlider.Value = Math.Min(leftSlider.Value, rightSlider.Value);
			middleSlider.Value = Math.Min(middleSlider.Value, rightSlider.Value);
			if (EndValueChanged != null)
				EndValueChanged(this, e);
		}
		private void middleSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (ValueChanged != null)
				ValueChanged(this, e);
		}
        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            var parent = element;
            while (parent != null)
            {
                var correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void thumbMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
	        m_isDragging = true;
            if (ThumbDragStart != null)
            {
                var slider = FindVisualParent<Slider>((UIElement)sender);
                if (slider != null && slider.Name == "middleSlider")
                    ThumbDragStart(this, new EventArgs());
            }
        }

        private void thumbMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
	        m_isDragging = false;
			var slider = FindVisualParent<Slider>((UIElement)sender);
			if (slider != null && slider.Name == "middleSlider")
			{
				middleSlider.Value = Math.Max(Math.Min(middleSlider.Value, rightSlider.Value), leftSlider.Value);
				if (ThumbDragEnd != null)
					ThumbDragEnd(this, new EventArgs());
            }
        }

		private void thumbMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (m_isDragging)
			{
				var slider = FindVisualParent<Slider>((UIElement) sender);
				if (slider != null && slider.Name == "middleSlider")
				{
					middleSlider.Value = Math.Max(Math.Min(middleSlider.Value, rightSlider.Value), leftSlider.Value);
					if (ThumbDragDelta != null)
						ThumbDragDelta(this, new EventArgs());
				}
			}
		}
	}
}