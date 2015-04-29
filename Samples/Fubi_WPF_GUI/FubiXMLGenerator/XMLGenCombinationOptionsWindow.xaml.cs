using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using FubiNET;
using Fubi_WPF_GUI.UpDownCtrls;
using System.Data;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{

	public partial class XMLGenCombinationOptionsWindow
	{
		// List of options for all states
		readonly List<XMLGenerator.CombinationStateOptions> m_stateOptions = new List<XMLGenerator.CombinationStateOptions>();

		// Template for the state grid
		const string StateGridXml =
			@"<Grid xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
					xmlns:uc=""clr-namespace:Fubi_WPF_GUI.UpDownCtrls;assembly=FUBI_WPF_GUI""
					HorizontalAlignment=""Left"" Margin=""12,138,0,0"" VerticalAlignment=""Top"" Width=""424"" Height=""64"">
				<Label Content=""Recognizers:"" Height=""28"" HorizontalAlignment=""Left"" Margin=""6,0,0,0"" Name=""label1"" VerticalAlignment=""Top"" Width=""103"" FontWeight=""Normal"" />
                <ComboBox ItemsSource=""{Binding}"" Height=""23"" HorizontalAlignment=""Left"" Margin=""9,31,0,0"" Name=""recognizers"" VerticalAlignment=""Top"" Width=""140"" >
                    <ComboBox.ItemTemplate>
                        <DataTemplate>
                            <CheckBox IsChecked=""{Binding IsChecked}"" Tag=""{Binding Name}"" TextOptions.TextFormattingMode=""Display"" Margin=""0,0,5,0"">
                                <TextBlock Text=""{Binding Name}""/>
                            </CheckBox>
                        </DataTemplate>
                    </ComboBox.ItemTemplate>
                </ComboBox>
				<Label Content=""- Select Recognizers -"" Height=""23"" HorizontalAlignment=""Left"" Margin=""10,31,0,0"" IsHitTestVisible=""False"" VerticalAlignment=""Top"" Width=""123"" FontWeight=""Normal"" Padding=""2""/>
                <uc:NumericUpDown Height=""23"" Margin=""161,30,0,0"" x:Name=""minDuration"" VerticalAlignment=""Top"" Minimum=""0"" Step=""0.1"" HorizontalAlignment=""Left"" Width=""60"" Value=""0"" DecimalPlaces=""2"" />
                <Label Content=""Min Duration:"" Height=""28"" HorizontalAlignment=""Left"" Margin=""154,0,0,0"" Name=""label2"" VerticalAlignment=""Top"" Width=""89"" FontWeight=""Normal"" />
                <uc:NumericUpDown HorizontalAlignment=""Left"" Height=""23"" Margin=""248,31,0,0"" x:Name=""maxDuration"" VerticalAlignment=""Top""  Width=""60"" Step=""0.1"" Value=""-1"" DecimalPlaces=""2"" UseMinusOneAsInvalid=""True"" />
                <uc:NumericUpDown HorizontalAlignment=""Left"" Height=""23"" Margin=""340,31,0,0"" x:Name=""timeForTransition"" VerticalAlignment=""Top""  Width=""60"" Step=""0.1"" DecimalPlaces=""2"" Value=""-1"" IsEnabled=""False"" UseMinusOneAsInvalid=""True"" />
                <Label Content=""Max Duration:"" Height=""28"" HorizontalAlignment=""Left"" Margin=""238,0,0,0"" Name=""label3"" VerticalAlignment=""Top"" Width=""89"" FontWeight=""Normal"" />
                <Label Content=""Transition Time:"" Height=""28"" HorizontalAlignment=""Left"" Margin=""321,0,0,0"" Name=""label4"" VerticalAlignment=""Top"" Width=""93"" FontWeight=""Normal"" />
                <Label Content=""s"" FontWeight=""Normal"" Height=""23"" HorizontalAlignment=""Left"" Margin=""221,30,0,0"" Name=""label12"" VerticalAlignment=""Top"" Width=""20"" Padding=""5,0"" />
                <Label Content=""s"" FontWeight=""Normal"" Height=""23"" HorizontalAlignment=""Left"" Margin=""307,31,0,0"" Name=""label13"" Padding=""5,0"" VerticalAlignment=""Top"" Width=""20"" />
                <Label Content=""s"" FontWeight=""Normal"" Height=""23"" HorizontalAlignment=""Left"" Margin=""399,31,0,0"" Name=""label14"" Padding=""5,0"" VerticalAlignment=""Top"" Width=""20"" />
			</Grid>";

		public bool HideInsteadOfClosing = true;
		public event XMLGenerator.RecognizerOptionsChanged OptionsChanged;
        // data table for the joints to render combobox
        private readonly List<DataTable> m_basicRecognizersTable = new List<DataTable>();


		public XMLGenCombinationOptionsWindow()
		{
			InitializeComponent();
			HideInsteadOfClosing = true;

            var d = new DataTable();
            d.Columns.Add("Name", typeof(string));
            d.Columns.Add("IsChecked", typeof(bool));
			d.ColumnChanged += recTableColumnChanged;
            m_basicRecognizersTable.Add(d);
            refreshGestureLists();
            recognizers.DataContext = m_basicRecognizersTable.Last();

			var trainTypes = Enum.GetNames(typeof(XMLGenerator.CombinationTrainingType));
			foreach (var type in trainTypes)
			{
				trainType.Items.Add(type);
			}

            foreach (var typeName in XMLGenerator.ToleranceTypeString)
            {
                timeToleranceType.Items.Add(typeName);
            }
            timeToleranceType.SelectedIndex = (int)XMLGenerator.ToleranceType.PlusMinus;

			// We start with one state
			m_stateOptions.Add(new XMLGenerator.CombinationStateOptions());
		}

		public void refreshGestureLists()
		{
            foreach (var dt in m_basicRecognizersTable)
            {
				// Remove rows if too many
				while (dt.Rows.Count > Fubi.getNumUserDefinedRecognizers())
					dt.Rows.RemoveAt(dt.Rows.Count-1);

				// Update/Add rows
	            for (uint p = 0; p < Fubi.getNumUserDefinedRecognizers(); p++)
	            {
		            string recName = Fubi.getUserDefinedRecognizerName(p);
		            if (p >= dt.Rows.Count)
		            {
			            // New entry
			            dt.Rows.Add(recName, false);
		            }
		            else if (dt.Rows[(int) p]["Name"] as string != recName)
		            {
			            // Replace entry with changed name
						dt.Rows[(int)p]["Name"] = recName;
						dt.Rows[(int)p]["IsChecked"] = false;
		            }
	            }
            }
		}

		private XMLGenerator.CombinationTrainingType getTrainType()
		{
			return (XMLGenerator.CombinationTrainingType)Enum.Parse(typeof(XMLGenerator.CombinationTrainingType), trainType.SelectedItem.ToString());
		}

		private void numStates_Changed(object sender, RoutedEventArgs e)
		{
			if (IsLoaded)
			{
				// Add additional states
				for (var i = m_stateOptions.Count; i < numStates.Value; ++i)
				{
					// Add template for state options
					m_stateOptions.Add(new XMLGenerator.CombinationStateOptions());

					// Create new GUI element from template string
					var stringReader = new StringReader(StateGridXml);
					var xmlReader = XmlReader.Create(stringReader);
					var newGrid = (Grid)XamlReader.Load(xmlReader);
					// Adapt margin
					var margin = stateOptionsGrid0.Margin;
					margin.Top += stateOptionsGrid0.Height * i;
					newGrid.Margin = margin;
					// Name with distinct pattern
					newGrid.Name = "stateOptionsGrid" + i;
					// Add data bindings
					foreach (Control ctrl in newGrid.Children)
					{
						if (ctrl.Name == "recognizers")
						{
							var box = ctrl as ComboBox;
							if (box != null)
							{
                                DataTable d = new DataTable();
                                d.Columns.Add("Name", typeof(string));
                                d.Columns.Add("IsChecked", typeof(bool));
								d.ColumnChanged += recTableColumnChanged;
                                m_basicRecognizersTable.Add(d);
                                refreshGestureLists();
                                box.DataContext = m_basicRecognizersTable.Last();
								box.SelectionChanged += recognizers_SelectionChanged;
							}
						}
					}
					// Add to surrounding grid and adapt its height
					mainGrid.Children.Add(newGrid);
					mainGrid.Height += newGrid.Height;
				}
				// And remove old ones
				for (var i = m_stateOptions.Count - 1; i >= numStates.Value; --i)
				{
					var oldGridName = "stateOptionsGrid" + i;
					foreach (UIElement el in mainGrid.Children)
					{
						if (el is Grid)
						{
							var g = el as Grid;
							if (g.Name == oldGridName)
							{
								mainGrid.Height -= g.Height;
								mainGrid.Children.Remove(g);
								m_stateOptions.RemoveAt(i);
								break;
							}
						}
					}
				}
				// Update activations
				updateStateOptionsActivation();
				checkOptions();
			}
		}

		private void recTableColumnChanged(object sender, DataColumnChangeEventArgs args)
		{
			if (args.Column.ColumnName == "IsChecked")
			{
				if (args.Row["Name"].ToString() == "all")
				{
					foreach (var b in args.Column.Table.AsEnumerable())
					{
						b.SetField("IsChecked", args.ProposedValue as bool?);
					}
				}
				checkOptions();
			}
		}

		private void updateStateOptionsActivation()
		{
			var type = getTrainType();
			var recognizersTrained = type == XMLGenerator.CombinationTrainingType.Gestures || type == XMLGenerator.CombinationTrainingType.GesturesAndTimes;
			var timesTrained = type == XMLGenerator.CombinationTrainingType.Times || type == XMLGenerator.CombinationTrainingType.GesturesAndTimes;

			// For the state options, only activate the options that we don't train
			foreach (UIElement el in mainGrid.Children)
			{
				if (el is Grid)
				{
					var g = el as Grid;
					foreach (Control ctrl in g.Children)
					{
						if (ctrl.Name == "recognizers")
							ctrl.IsEnabled = !recognizersTrained;
						else if (ctrl.Name == "minDuration" || ctrl.Name == "maxDuration")
							ctrl.IsEnabled = !timesTrained;
                        else if (ctrl.Name == "timeForTransition")
						{
							if (g.Name == "stateOptionsGrid" + (m_stateOptions.Count - 1))
								// No transition for the last state
								ctrl.IsEnabled = false;
							else
								ctrl.IsEnabled = !timesTrained;
						}
					}
				}
			}
		}

		private void trainType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (IsLoaded)
			{
				var type = getTrainType();
				var timesTrained = type == XMLGenerator.CombinationTrainingType.Times || type == XMLGenerator.CombinationTrainingType.GesturesAndTimes;

				// Only activate the time tolerance fields if we are training time
				timeToleranceType.IsEnabled = timeTolerance.IsEnabled = transitionTolerance.IsEnabled = timesTrained;
				if (!timesTrained)
				{
					// When they are deactivated they should also have value 0
					timeTolerance.Value = transitionTolerance.Value = 0;
				}

				updateStateOptionsActivation();
				checkOptions();
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (HideInsteadOfClosing)
			{
				Visibility = Visibility.Hidden;
				e.Cancel = true;
			}
		}

		private void closeButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
		}

		public void checkOptions()
		{
			var valid = true;

			// We only need to check whether at least one recognizer is selected for each state if those are not trained
			//  all other options can't get wrong
			var type = getTrainType();
			if (type == XMLGenerator.CombinationTrainingType.None || type == XMLGenerator.CombinationTrainingType.Times)
			{
				foreach (UIElement el in mainGrid.Children)
				{
					if (el is Grid)
					{
						var g = el as Grid;
						var recognizerForEachStateSelected = true;
						foreach (var box in (from Control cl in g.Children where cl.GetType() == typeof (ComboBox) select cl as ComboBox))
						{
							if (box != null && box.Items.Cast<DataRowView>().All(rowView => rowView["IsChecked"] as bool? == false))
							{
								recognizerForEachStateSelected = false;
								break;
							}
						}
						if (!recognizerForEachStateSelected)
						{
							hintsTextBlock.Text = "Please select at least one recognizer for each state or activate training!";
							hintsTextBlock.Foreground = Brushes.OrangeRed;
							valid = false;
						}
					}
				}
			}

			if (valid)
			{
				hintsTextBlock.Text = "Options valid.";
				hintsTextBlock.Foreground = Brushes.LightGreen;
			}

			if (OptionsChanged != null)
				OptionsChanged(this, new XMLGenerator.RecognizerOptionsChangedArgs(valid));
		}

		public void getOptions(ref XMLGenerator.CombinationOptions combinationOptions)
		{
			// General options
			combinationOptions.TimeTolerance = timeTolerance.Value;
            combinationOptions.TimeToleranceType = timeToleranceType.SelectedItem.ToString();
			combinationOptions.TransitionTolerance = transitionTolerance.Value;
			combinationOptions.TrainType = getTrainType();

			// Per state options
			combinationOptions.States.Clear();
			for (var i = 0; i < m_stateOptions.Count; ++i)
			{
				var gridName = "stateOptionsGrid" + i;
				foreach (UIElement el in mainGrid.Children)
				{
					if (el is Grid)
					{
						var g = el as Grid;
						if (g.Name == gridName)
						{
							var state = new XMLGenerator.CombinationStateOptions();
							foreach (Control ctrl in g.Children)
							{
								if (ctrl.Name == "recognizers" && ctrl is ComboBox)
								{
									var cb = ctrl as ComboBox;
									if (cb.DataContext is DataTable)
									{
										var dt = cb.DataContext as DataTable;
										foreach (var row in dt.Select("IsChecked=true"))
										{
											state.Recognizers.Add(row["Name"].ToString());	
										}
									}
								}
								else if (ctrl.Name == "minDuration" && ctrl is NumericUpDown)
								{
									var n = ctrl as NumericUpDown;
									state.MinDuration = n.Value;
								}
								else if (ctrl.Name == "maxDuration" && ctrl is NumericUpDown)
								{
									var n = ctrl as NumericUpDown;
									state.MaxDuration = n.Value;
								}
								else if (ctrl.Name == "timeForTransition" && ctrl is NumericUpDown)
								{
									var n = ctrl as NumericUpDown;
									state.TimeForTransition = n.Value;
								}
							}
							combinationOptions.States.Add(state);
							break;
						}
					}
				}
			}
		}

		private void recognizers_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = sender as ComboBox;
			if (box != null)
				box.SelectedItem = null;
		}
	}
}
