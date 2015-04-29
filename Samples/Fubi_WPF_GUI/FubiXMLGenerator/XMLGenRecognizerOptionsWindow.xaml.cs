using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using FubiNET;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{

	public partial class XMLGenRecognizerOptionsWindow
	{
        public List<XMLGenerator.RelativeJoint> SelectedJoints = new List<XMLGenerator.RelativeJoint>();
		public bool HideInsteadOfClosing = true;

		public event XMLGenerator.RecognizerOptionsChanged OptionsChanged;

		public XMLGenRecognizerOptionsWindow(XMLGenerator.RecognizerType type)
		{
			HideInsteadOfClosing = true;
			InitializeComponent();

			measuringUnit.Items.Add("millimeter"); // No actual body measurement
			measuringUnit.SelectedIndex = 0;
			foreach (FubiUtils.BodyMeasurement measure in Enum.GetValues(typeof(FubiUtils.BodyMeasurement)))
			{
				if (measure != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
					measuringUnit.Items.Add(FubiUtils.getBodyMeasureName(measure));
			}

			foreach (var measureName in Enum.GetNames(typeof(FubiUtils.DistanceMeasure)))
			{
				distanceMeasure.Items.Add(measureName);
			}
			distanceMeasure.SelectedIndex = (int)FubiUtils.DistanceMeasure.Euclidean;


			foreach (var typeName in XMLGenerator.ToleranceTypeString)
			{
				toleranceXType.Items.Add(typeName);
				toleranceYType.Items.Add(typeName);
				toleranceZType.Items.Add(typeName);
				toleranceDistType.Items.Add(typeName);
				toleranceSpeedType.Items.Add(typeName);
				toleranceFingerCountType.Items.Add(typeName);
			}
			toleranceXType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;
			toleranceYType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;
			toleranceZType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;
			toleranceDistType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;
			toleranceSpeedType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;
			toleranceFingerCountType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;

			Type = type;
		}

		private XMLGenerator.RecognizerType m_type;
		public XMLGenerator.RecognizerType Type
		{
			get { return m_type; }
			set
			{
				// First deactivate all possibly optional controls
				m_type = value;
				speed.IsEnabled = false;
				toleranceSpeedType.IsEnabled = false;
				maxAngle.IsEnabled = false;
				toleranceX.IsEnabled = false;
				toleranceY.IsEnabled = false;
				toleranceZ.IsEnabled = false;
				toleranceDist.IsEnabled = false;
				toleranceXType.IsEnabled = false;
				toleranceYType.IsEnabled = false;
				toleranceZType.IsEnabled = false;
				toleranceDistType.IsEnabled = false;
				fingerCount.IsEnabled = false;
				toleranceFingerCountType.IsEnabled = false;
				medianWindow.IsEnabled = false;
				measuringUnit.IsEnabled = false;
				maxDistance.IsEnabled = false;
				distanceMeasure.IsEnabled = false;
				useOrientations.IsEnabled = false;
				aspectInvariant.IsEnabled = false;
				xActive.IsEnabled = false;
				yActive.IsEnabled = false;
				zActive.IsEnabled = false;
				// Activate the ones only deactivated for finger counts
				localTrans.IsEnabled = true;
				// Now only activate the ones we need
				switch (m_type)
				{
					case XMLGenerator.RecognizerType.LinearMovement:
						speed.IsEnabled = true;
						toleranceSpeedType.IsEnabled = true;
						maxAngle.IsEnabled = true;
                        toleranceDist.IsEnabled = true;
						toleranceDistType.IsEnabled = true;
                        measuringUnit.IsEnabled = true;
						xActive.IsEnabled = true;
						yActive.IsEnabled = true;
						zActive.IsEnabled = true;
						break;
					case XMLGenerator.RecognizerType.FingerCount:
						fingerCount.IsEnabled = true;
						toleranceFingerCountType.IsEnabled = true;
						medianWindow.IsEnabled = true;
						localTrans.IsEnabled = false;
						break;
					case XMLGenerator.RecognizerType.TemplateRecording:
						maxAngle.IsEnabled = true;
						measuringUnit.IsEnabled = true;
						maxDistance.IsEnabled = true;
						distanceMeasure.IsEnabled = true;
						useOrientations.IsEnabled = true;
						aspectInvariant.IsEnabled = true;
						xActive.IsEnabled = true;
						yActive.IsEnabled = true;
						zActive.IsEnabled = true;
						break;
					case XMLGenerator.RecognizerType.JointOrientation:
						toleranceX.IsEnabled = true;
						toleranceY.IsEnabled = true;
						toleranceZ.IsEnabled = true;
						toleranceXType.IsEnabled = true;
						toleranceYType.IsEnabled = true;
						toleranceZType.IsEnabled = true;
						maxAngle.IsEnabled = true;
						break;
					case XMLGenerator.RecognizerType.JointRelation:
						toleranceX.IsEnabled = true;
						toleranceY.IsEnabled = true;
						toleranceZ.IsEnabled = true;
						toleranceDist.IsEnabled = true;
						toleranceXType.IsEnabled = true;
						toleranceYType.IsEnabled = true;
						toleranceZType.IsEnabled = true;
						toleranceDistType.IsEnabled = true;
						measuringUnit.IsEnabled = true;
						break;
					default:
						toleranceX.IsEnabled = true;
						toleranceY.IsEnabled = true;
						toleranceZ.IsEnabled = true;
						toleranceXType.IsEnabled = true;
						toleranceYType.IsEnabled = true;
						toleranceZType.IsEnabled = true;
						break;
				}

                while (SelectedJoints.Count > 1 && m_type != XMLGenerator.RecognizerType.TemplateRecording)
                {
                    var selectedJoint = SelectedJoints[SelectedJoints.Count-1];
                    SelectedJoints.Remove(selectedJoint);
                    uncheckJointBox(selectedJoint.Main);
                    uncheckJointBox(selectedJoint.Relative);
                }
				if (SelectedJoints.Count == 1 && SelectedJoints[0].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS
                    && (m_type == XMLGenerator.RecognizerType.JointOrientation || m_type == XMLGenerator.RecognizerType.AngularMovement || m_type == XMLGenerator.RecognizerType.FingerCount))
				{
                    uncheckJointBox(SelectedJoints[0].Relative);
                    SelectedJoints[0].Relative = FubiUtils.SkeletonJoint.NUM_JOINTS;
				}
				if (SelectedJoints.Count == 1 && m_type == XMLGenerator.RecognizerType.FingerCount)
				{
					var selectedJoint = SelectedJoints[0].Main;
					if (selectedJoint != FubiUtils.SkeletonJoint.LEFT_HAND
						&& selectedJoint != FubiUtils.SkeletonJoint.RIGHT_HAND
                        && selectedJoint != (FubiUtils.SkeletonJoint)FubiUtils.SkeletonHandJoint.PALM)
					{
                        uncheckJointBox(selectedJoint);
                        SelectedJoints.Clear();
					}
				}

                updateMeasuringUnit();

				checkSelections();
			}
		}

        private bool m_useHand;
        public bool UseHand
        {
            get { return m_useHand; }
            set
            {
                while (SelectedJoints.Count > 0)
                {
                    var selectedJoint = SelectedJoints[SelectedJoints.Count - 1];
                    SelectedJoints.Remove(selectedJoint);
                    uncheckJointBox(selectedJoint.Main);
                    uncheckJointBox(selectedJoint.Relative);
                }
                m_useHand = value;
                if (m_useHand == true)
                {
                    skeletonJoints.Visibility = Visibility.Hidden;
                    handJoints.Visibility = Visibility.Visible;
                }
                else
                {
                    skeletonJoints.Visibility = Visibility.Visible;
                    handJoints.Visibility = Visibility.Hidden;
                }
            }
        }

        private void uncheckJointBox(FubiUtils.SkeletonJoint joint)
        {
            var box = (CheckBox)FindName(UseHand ? FubiUtils.getHandJointName((FubiUtils.SkeletonHandJoint)joint)  : FubiUtils.getJointName(joint));
            if (box != null)
            {
                box.Background = Brushes.White;
                box.IsChecked = false;
            }
        }

        private void updateMeasuringUnit()
        {
            switch (m_type)
            {
                case XMLGenerator.RecognizerType.JointOrientation:
                    xMeasureLabel.Content = yMeasureLabel.Content = zMeasureLabel.Content = "θ";
                    toleranceX.Step = toleranceY.Step = toleranceZ.Step = 1;
                    toleranceX.DecimalPlaces = toleranceY.DecimalPlaces = toleranceZ.DecimalPlaces = 1;
                    toleranceX.Value = Math.Min(toleranceX.Value, 180);
                    toleranceY.Value = Math.Min(toleranceY.Value, 180);
                    toleranceZ.Value = Math.Min(toleranceZ.Value, 180);
                    toleranceX.Maximum = toleranceY.Maximum = toleranceZ.Maximum = 180;
                    toleranceX.Value = Math.Min(toleranceX.Value, 180);
                    break;
                case XMLGenerator.RecognizerType.AngularMovement:
                    xMeasureLabel.Content = yMeasureLabel.Content = zMeasureLabel.Content = "θ/s";
                    toleranceX.Step = toleranceY.Step = toleranceZ.Step = 10;
                    toleranceX.DecimalPlaces = toleranceY.DecimalPlaces = toleranceZ.DecimalPlaces = 1;
                    toleranceX.Value = Math.Min(toleranceX.Value, 720);
                    toleranceY.Value = Math.Min(toleranceY.Value, 720);
                    toleranceZ.Value = Math.Min(toleranceZ.Value, 720);
                    toleranceX.Maximum = toleranceY.Maximum = toleranceZ.Maximum = 720;
                    break;
                default:
                    if (measuringUnit.SelectedItem == null || measuringUnit.SelectedItem.ToString() == "millimeter")
                    {
                        distMeasureLabel.Content = xMeasureLabel.Content = yMeasureLabel.Content = zMeasureLabel.Content = "mm";
                        toleranceDist.Step = toleranceX.Step = toleranceY.Step = toleranceZ.Step = 100;
                        toleranceDist.DecimalPlaces = toleranceX.DecimalPlaces = toleranceY.DecimalPlaces = toleranceZ.DecimalPlaces = 0;
                        toleranceDist.Maximum = toleranceX.Maximum = toleranceY.Maximum = toleranceZ.Maximum = 9999;
                    }
                    else
                    {
                        distMeasureLabel.Content = xMeasureLabel.Content = yMeasureLabel.Content = zMeasureLabel.Content = measuringUnit.SelectedItem.ToString();
                        toleranceDist.Step = toleranceX.Step = toleranceY.Step = toleranceZ.Step = 0.1;
                        toleranceDist.DecimalPlaces = toleranceX.DecimalPlaces = toleranceY.DecimalPlaces = toleranceZ.DecimalPlaces = 2;
                        toleranceDist.Maximum = toleranceX.Maximum = toleranceY.Maximum = toleranceZ.Maximum = 99.99;
                    }
                    break;
            }

        }
		
		private void jointSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (IsLoaded && sender.GetType() == typeof(CheckBox))
			{
				var box = (CheckBox)sender;
				var joint = FubiUtils.getJointID(box.Name);
                if (UseHand)
                {
                    var handJoint = FubiUtils.getHandJointID(box.Name);
                    if (handJoint != FubiUtils.SkeletonHandJoint.NUM_JOINTS)
                        joint = (FubiUtils.SkeletonJoint)handJoint;
                }
				if (joint != FubiUtils.SkeletonJoint.NUM_JOINTS)
				{
					if (box.IsChecked == true)
					{
						if (SelectedJoints.Count == 0 
                            || (Type == XMLGenerator.RecognizerType.TemplateRecording && SelectedJoints[SelectedJoints.Count-1].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS))
						{
							if (m_type == XMLGenerator.RecognizerType.FingerCount
								&& ((!m_useHand && joint != FubiUtils.SkeletonJoint.LEFT_HAND && joint != FubiUtils.SkeletonJoint.RIGHT_HAND)
                                    || (m_useHand && joint != (FubiUtils.SkeletonJoint) FubiUtils.SkeletonHandJoint.PALM)))
							{
								box.IsChecked = false;
								showToolTipOnElement(box, "You can only select the left/right hand or the palm for a " + Type.ToString());
							}
							else
							{
								SelectedJoints.Add(new XMLGenerator.RelativeJoint(joint));
								box.Background = Brushes.LightGreen;
							}
						}
						else if (SelectedJoints[SelectedJoints.Count - 1].Relative == FubiUtils.SkeletonJoint.NUM_JOINTS
							&& ((SelectedJoints.Count == 1 && (Type == XMLGenerator.RecognizerType.JointRelation || Type == XMLGenerator.RecognizerType.LinearMovement))
								|| Type == XMLGenerator.RecognizerType.TemplateRecording))
						{
							SelectedJoints[SelectedJoints.Count - 1].Relative = joint;
							box.Background = Brushes.Yellow;
						}
						else
						{
							box.IsChecked = false;
							showToolTipOnElement(box, "You cannot select more joints for a " + Type.ToString());
						}
					}
					else // box.IsChecked == false
					{
                        box.Background = Brushes.White;
                        if (SelectedJoints.Count == 1)
                        {
                            if (SelectedJoints[0].Main == joint)
                            {
                                if (SelectedJoints[0].Relative == FubiUtils.SkeletonJoint.NUM_JOINTS)
                                    SelectedJoints.Clear();
                                else
                                {
                                    SelectedJoints[0].Main = SelectedJoints[0].Relative;
									SelectedJoints[0].Relative = FubiUtils.SkeletonJoint.NUM_JOINTS;
                                    var jointName = FubiUtils.getJointName(SelectedJoints[0].Main);
                                    var otherBox = (CheckBox)FindName(jointName);
                                    if (otherBox != null)
                                        otherBox.Background = Brushes.LightGreen;
                                }
                            }
                            else if (SelectedJoints[0].Relative == joint)
                            {
                                SelectedJoints[0].Relative = FubiUtils.SkeletonJoint.NUM_JOINTS;
                            }
                        }
						else // SelectedJoints.Count > 1
                        {
                            var relJoint = SelectedJoints.Find(j => j.Main == joint);
                            if (relJoint != null)
                            {
                                if (relJoint.Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
                                    uncheckJointBox(relJoint.Relative);
                                SelectedJoints.Remove(relJoint);
                            }
                            relJoint = SelectedJoints.Find(j => j.Relative == joint);
                            if (relJoint != null)
                                relJoint.Relative = FubiUtils.SkeletonJoint.NUM_JOINTS;
                        }
					}
				}
				checkSelections();
			}
		}

		private void optionsChanged(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
				checkSelections();
		}

		public void checkSelections()
		{
			var valid = false;
			if (SelectedJoints.Count == 0)
			{
				hintsLabel.Content = "Please select at least one joint!";
				hintsLabel.Foreground = Brushes.OrangeRed;
			}
			else if (Type == XMLGenerator.RecognizerType.FingerCount)
			{
				// Finger counts don't need more settings to be checked (min/max can't get out of range)
				valid = true;
			}
			else if ((Type == XMLGenerator.RecognizerType.LinearMovement || Type == XMLGenerator.RecognizerType.TemplateRecording)
				&& xActive.IsChecked == false && yActive.IsChecked == false && zActive.IsChecked == false)
			{
				hintsLabel.Content = "Please activate at least one axis!";
				hintsLabel.Foreground = Brushes.OrangeRed;
			}
			else if (Type == XMLGenerator.RecognizerType.LinearMovement)
			{
				if (speed.Value < 0 && maxAngle.Value < 0 && toleranceDist.Value < 0)
				{
					hintsLabel.Content = "Please set speed, distance or angle tolerance!";
					hintsLabel.Foreground = Brushes.OrangeRed;
				}
				else
					// Linear movements don't need x,y,z tolerances
					valid = true;
			}
            else if (Type == XMLGenerator.RecognizerType.TemplateRecording)
            {
                valid = true;
            }
			else if (toleranceX.Value < 0 && toleranceY.Value < 0 && toleranceZ.Value < 0)
			{
				if (Type == XMLGenerator.RecognizerType.JointOrientation)
				{
					if (maxAngle.Value < 0)
					{
						hintsLabel.Content = "Please select at least one axis tolerance or the max angle tolerance!";
						hintsLabel.Foreground = Brushes.OrangeRed;
					}
					else
						valid = true;
				}
				else if (Type == XMLGenerator.RecognizerType.JointRelation && toleranceDist.Value >= 0)
				{
					valid = true;
				}
				else
				{
					hintsLabel.Content = "Please select at least one axis tolerance!";
					hintsLabel.Foreground = Brushes.OrangeRed;
				}
			}
			else
				valid = true;
			
			if (valid)
			{
				hintsLabel.Content = "Options valid.";
				hintsLabel.Foreground = Brushes.LightGreen;
			}
			if (OptionsChanged != null)
				OptionsChanged(this, new XMLGenerator.RecognizerOptionsChangedArgs(valid));
		}

        private void measuringUnitChanged(object sender, RoutedEventArgs e)
		{
            if (IsLoaded)
            {
                updateMeasuringUnit();
                checkSelections();
            }
		}

		public void getOptions(ref XMLGenerator.RecognizerOptions options)
		{
			options.SelectedJoints = SelectedJoints;
			options.ToleranceX = toleranceX.Value;
			options.ToleranceY = toleranceY.Value;
			options.ToleranceZ = toleranceZ.Value;
			options.ToleranceDist = toleranceDist.Value;
			options.ToleranceSpeed = speed.Value;
			options.ToleranceXType = toleranceXType.SelectedItem.ToString();
			options.ToleranceYType = toleranceYType.SelectedItem.ToString();
			options.ToleranceZType = toleranceZType.SelectedItem.ToString();
			options.ToleranceDistType = toleranceDistType.SelectedItem.ToString();
			options.ToleranceSpeedType = toleranceSpeedType.SelectedItem.ToString();
			options.MaxAngleDifference = maxAngle.Value;
			options.ToleranceCountType = toleranceFingerCountType.SelectedItem.ToString();
			options.ToleranceCount = (uint)fingerCount.Value;
			options.MedianWindowSize = (uint)medianWindow.Value;
			options.Local = localTrans.IsChecked == true;
			options.Filtered = filtered.IsChecked == true;
			options.MeasuringUnit = FubiUtils.getBodyMeasureID(measuringUnit.SelectedValue.ToString());
			options.MaxDistance = maxDistance.Value;
			options.DistanceMeasure = (FubiUtils.DistanceMeasure)Enum.Parse(typeof(FubiUtils.DistanceMeasure), distanceMeasure.SelectedItem.ToString());
			options.IgnoreAxes = 0;
			if (xActive.IsChecked == false)
				options.IgnoreAxes |= (uint)FubiUtils.CoordinateAxis.X;
			if (yActive.IsChecked == false)
				options.IgnoreAxes |= (uint)FubiUtils.CoordinateAxis.Y;
			if (zActive.IsChecked == false)
				options.IgnoreAxes |= (uint)FubiUtils.CoordinateAxis.Z;
			options.UseOrientations = useOrientations.IsChecked == true;
			options.AspectInvariant = aspectInvariant.IsChecked == true;
		}

		private void closeButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
		}
		
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (HideInsteadOfClosing)
			{
				Visibility = Visibility.Hidden;
				e.Cancel = true;
			}
		}

		private void showToolTipOnElement(FrameworkElement element, string text)
		{
			// Show a tooltip that will be automatically removed after hiding
			ToolTip tip = new ToolTip
			{
				Content = text,
				IsOpen = true,

				StaysOpen = false								
			};
			tip.Closed += (sender0, args0) =>
			{
				element.ToolTip = null;
			};
			element.ToolTip = tip;
		}
	}
}
