using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Threading;
using System.Data;
using System.Globalization;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using FubiNET;
using Fubi_WPF_GUI.Properties;
using Fubi_WPF_GUI.UpDownCtrls;
using Microsoft.Win32;
using Fubi_WPF_GUI.FubiXMLGenerator;
using System.Windows.Data;

namespace Fubi_WPF_GUI
{
    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Double.Parse(value.ToString()) * Double.Parse(parameter.ToString());
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Double.Parse(value.ToString()) / Double.Parse(parameter.ToString());
        }
    }

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // Additional windows and dialogs (created on demand)
        private RecognizerStatsWindow m_statsWindow;
        private OpenFileDialog m_openRecDlg;
        private XMLGenRecognizerOptionsWindow m_xmlGenOptionsWnd;
        private XMLGenCombinationOptionsWindow m_xmlGenCombOptionsWnd;

        // data table for the joints to render combobox
        private readonly DataTable m_jointsToRenderDt = new DataTable();

        // The thread for updating Fubi separately to the GUI
        private readonly Thread m_fubiThread;
        private bool m_running;
        // Delegate for triggering gui updates from separate threads
        private delegate void NoArgDelegate();
        private delegate void OneStringDelegate(string str);
        private delegate void TwoStringDelegate(string str1, string str2);
        // Update cycles for whenever the GUI triggers changes for Fubi
        enum UpdateCycle
        {
            UpToDate = 0,
            Triggered = 1,
            FubiUpdated = 2
        };
        UpdateCycle m_clearRecognizers = UpdateCycle.UpToDate;
        UpdateCycle m_switchSensor = UpdateCycle.UpToDate;
        UpdateCycle m_switchFingerSensor = UpdateCycle.UpToDate;
        UpdateCycle m_resetTracking = UpdateCycle.UpToDate;

        // Threading
        public readonly object LockFubiUpdate = new object(); // identifies sections that only one thread may enter

        // The current Fubi settings (first modified by the GUI and then taken by Fubi)
        FubiUtils.SensorType m_selectedSensor = FubiUtils.SensorType.NONE;
        FubiUtils.FingerSensorType m_selectedFingerSensor = FubiUtils.FingerSensorType.NONE;
        FubiUtils.ImageType m_selectedImageType = FubiUtils.ImageType.Depth;
        FubiUtils.ImageNumChannels m_numChannels = FubiUtils.ImageNumChannels.C4; // influences the initial buffer size!
        int m_width = 640, m_height = 480;	// influences the initial buffer size!
        FubiUtils.DepthImageModification m_selectedDepthMod = FubiUtils.DepthImageModification.UseHistogram;
        int m_jointsToRender = (int)FubiUtils.JointsToRender.ALL_JOINTS;
        int m_renderOptions = (int)FubiUtils.RenderOptions.Default;
        bool m_irStreamActive;
        bool m_registerStreams = true;
        uint m_fingerSensorImageIndex = 0;

        // Buffers for the currently displayed streams (needs to be resized when changing stream options)
        private static byte[] s_buffer, s_fingerSensorBuffer;

        // Dictionaries for keeping the current gesture states
        private readonly Dictionary<uint, Dictionary<uint, bool>> m_currentGestures = new Dictionary<uint, Dictionary<uint, bool>>();
        private readonly Dictionary<uint, Dictionary<uint, bool>> m_currentFingerGestures = new Dictionary<uint, Dictionary<uint, bool>>();
        private readonly Dictionary<uint, Dictionary<uint, bool>> m_currentPredefinedGestures = new Dictionary<uint, Dictionary<uint, bool>>();
        // And a list of all gestures
        public enum GestureType
        {
            USER_DEFINED_GESTURE,
            USER_DEFINED_COMBINATION,
            PREDEFINED_GESTURE,
            PREDEFINED_COMBINATION
        };
        public static Dictionary<string, GestureType> GestureList = new Dictionary<string, GestureType>();

        // Mouse and keyboard integration and control
        private readonly FubiMouseKeyboard m_fubiMouseKeyboard = new FubiMouseKeyboard();
        readonly KeyboardListener m_kListener = new KeyboardListener();
        private bool m_controlMouse;
        private bool m_enableKeyMouseBinding;

        readonly GridLength[] m_rowHeights;

        readonly XMLGenerator m_xmlGenerator = new XMLGenerator();
        private CancellationTokenSource m_cancelXmlGenToken = null;

        //#ifdef ADD_RP_2015
        private AviClass aviFile = new AviClass();
        private int lastNumUsers = 0;
        private string recordSkeletonFileName;
        private Thread backgroundThread;
        private bool isHand = false;
        private bool recordingStarted = false;

        //#endif


        public MainWindow()
        {
            // Set culuture to have a common number format, ...
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            // Add a handler for all unhandled exceptions
            // Not very specific, but the easies way to catch those, also catches all other unexpected exceptions
            Dispatcher.UnhandledException += (sender, args) =>
            {
                showWarnMsg(args.Exception.InnerException != null ? args.Exception.InnerException.Message : args.Exception.Message, "Error");
                args.Handled = true;
            };

            InitializeComponent();

            m_rowHeights = new GridLength[mainGrid.RowDefinitions.Count];
            m_rowHeights[0] = mainGrid.RowDefinitions[0].Height;
            m_rowHeights[m_rowHeights.Length - 1] = mainGrid.RowDefinitions[m_rowHeights.Length - 1].Height;
            updateExpanderConstrains();

            enableLoadingCircle();

            initGUIOptions();

            m_running = true;
            m_switchSensor = UpdateCycle.Triggered;
            m_switchFingerSensor = UpdateCycle.Triggered;
            m_fubiThread = new Thread(fubiMain);

            keyMouseBindings.DataContext = m_fubiMouseKeyboard.Bindings;
            keyMouseGestures.ItemsSource = new Dictionary<string, GestureType>();

            m_kListener.KeyUp += keyPressedHandler;
        }

        private void initGUIOptions()
        {
            var mods = Enum.GetNames(typeof(FubiUtils.DepthImageModification));
            var selectedModName = Enum.GetName(typeof(FubiUtils.DepthImageModification), m_selectedDepthMod);
            foreach (var mod in mods)
            {
                var index = depthModComboBox.Items.Add(mod);
                if (index > -1 && mod == selectedModName)
                {
                    depthModComboBox.SelectedIndex = index;
                }
            }

            var imageTypes = Enum.GetNames(typeof(FubiUtils.ImageType));
            var selectedType = Enum.GetName(typeof(FubiUtils.ImageType), m_selectedImageType);
            foreach (var imageType in imageTypes)
            {
                var index = imageStreamComboBox.Items.Add(imageType);
                if (index > -1 && imageType == selectedType)
                {
                    imageStreamComboBox.SelectedIndex = index;
                }
            }

            m_jointsToRenderDt.Columns.Add("Name", typeof(string));
            m_jointsToRenderDt.Columns.Add("IsChecked", typeof(bool));
            var jointTypes = Enum.GetNames(typeof(FubiUtils.JointsToRender));
            foreach (var jointType in jointTypes)
            {
                m_jointsToRenderDt.Rows.Add(jointType, true);
            }
            jointsToRenderCb.DataContext = m_jointsToRenderDt;

            var availableSensors = new List<string> { Enum.GetName(typeof(FubiUtils.SensorType), FubiUtils.SensorType.NONE) };
            var avSensors = Fubi.getAvailableSensors();
            m_selectedSensor = FubiUtils.SensorType.NONE;
            if ((avSensors & (int)FubiUtils.SensorType.KINECTSDK2) != 0)
            {
                if (m_selectedSensor == FubiUtils.SensorType.NONE)
                {
                    m_selectedSensor = FubiUtils.SensorType.KINECTSDK2;
                }
                availableSensors.Add(Enum.GetName(typeof(FubiUtils.SensorType), FubiUtils.SensorType.KINECTSDK2));
            }
            if ((avSensors & (int)FubiUtils.SensorType.OPENNI2) != 0)
            {
                m_selectedSensor = FubiUtils.SensorType.OPENNI2;
                availableSensors.Add(Enum.GetName(typeof(FubiUtils.SensorType), m_selectedSensor));
            }
            if ((avSensors & (int)FubiUtils.SensorType.KINECTSDK) != 0)
            {
                if (m_selectedSensor == FubiUtils.SensorType.NONE)
                    m_selectedSensor = FubiUtils.SensorType.KINECTSDK;
                availableSensors.Add(Enum.GetName(typeof(FubiUtils.SensorType), FubiUtils.SensorType.KINECTSDK));
            }
            if ((avSensors & (int)FubiUtils.SensorType.OPENNI1) != 0)
            {
                if (m_selectedSensor == FubiUtils.SensorType.NONE)
                    m_selectedSensor = FubiUtils.SensorType.OPENNI1;
                availableSensors.Add(Enum.GetName(typeof(FubiUtils.SensorType), FubiUtils.SensorType.OPENNI1));
            }

            var selectedName = Enum.GetName(typeof(FubiUtils.SensorType), m_selectedSensor);
            foreach (var sType in availableSensors)
            {
                var index = sensorSelectionComboBox.Items.Add(sType);
                if (index > -1 && sType == selectedName)
                {
                    sensorSelectionComboBox.SelectedIndex = index;
                }
            }

            var availableFingerSensors = new List<string>();
            var selectedFingerSName = Enum.GetName(typeof(FubiUtils.FingerSensorType), FubiUtils.FingerSensorType.NONE);
            availableFingerSensors.Add(selectedFingerSName);
            var avFSensors = Fubi.getAvailableFingerSensorTypes();
            m_selectedFingerSensor = FubiUtils.FingerSensorType.NONE;
            if ((avFSensors & (int)FubiUtils.FingerSensorType.LEAP) != 0)
            {
                var name = Enum.GetName(typeof(FubiUtils.FingerSensorType), FubiUtils.FingerSensorType.LEAP);
                if (m_selectedSensor == FubiUtils.SensorType.NONE)
                {
                    selectedFingerSName = name;
                    m_selectedFingerSensor = FubiUtils.FingerSensorType.LEAP;
                }
                availableFingerSensors.Add(name);
            }
            foreach (var sType in availableFingerSensors)
            {
                var index = fingerSensorComboBox.Items.Add(sType);
                if (index > -1 && sType == selectedFingerSName)
                {
                    fingerSensorComboBox.SelectedIndex = index;
                }
            }

            button4.IsEnabled = m_selectedSensor == FubiUtils.SensorType.OPENNI1;

            var recTypes = Enum.GetNames(typeof(XMLGenerator.RecognizerType));
            foreach (var type in recTypes)
            {
                xmlGenRecTypeComboBox.Items.Add(type);
            }
        }

        public void showWarnMsg(string message, string caption)
        {
            MessageBox.Show(this, message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void refreshGestureList()
        {
            GestureList.Clear();

            lock (LockFubiUpdate)
            {
                // User defined combinations
                for (uint i = 0; i < Fubi.getNumUserDefinedCombinationRecognizers(); ++i)
                {
                    if (!GestureList.ContainsKey(Fubi.getUserDefinedCombinationRecognizerName(i)))
                        GestureList.Add(Fubi.getUserDefinedCombinationRecognizerName(i), GestureType.PREDEFINED_COMBINATION);
                }
                // User defined gestures/postures
                for (uint i = 0; i < Fubi.getNumUserDefinedRecognizers(); ++i)
                {
                    if (!GestureList.ContainsKey(Fubi.getUserDefinedRecognizerName(i)))
                        GestureList.Add(Fubi.getUserDefinedRecognizerName(i), GestureType.USER_DEFINED_GESTURE);
                }
                // Predefined combinations
                for (uint i = 0; i < (uint)FubiPredefinedGestures.Combinations.NUM_COMBINATIONS; ++i)
                {
                    if (!GestureList.ContainsKey(FubiPredefinedGestures.getCombinationName((FubiPredefinedGestures.Combinations)i)))
                        GestureList.Add(FubiPredefinedGestures.getCombinationName((FubiPredefinedGestures.Combinations)i),
                            GestureType.PREDEFINED_COMBINATION);
                }
                // Predefined postures
                for (uint i = 0; i < (uint)FubiPredefinedGestures.Postures.NUM_POSTURES; ++i)
                {
                    if (!GestureList.ContainsKey(FubiPredefinedGestures.getPostureName((FubiPredefinedGestures.Postures)i)))
                        GestureList.Add(FubiPredefinedGestures.getPostureName((FubiPredefinedGestures.Postures)i),
                            GestureType.PREDEFINED_GESTURE);
                }

            }
            keyMouseGestures.ItemsSource = GestureList;

            if (m_xmlGenCombOptionsWnd != null)
                m_xmlGenCombOptionsWnd.refreshGestureLists();
        }

        private void fubiMain(object filterOptions)
        {
            var rgbOptions = (m_irStreamActive && m_selectedSensor != FubiUtils.SensorType.KINECTSDK2) ? new FubiUtils.StreamOptions(-1, -1, -1) : new FubiUtils.StreamOptions();
            var irOptions = (m_irStreamActive || m_selectedSensor == FubiUtils.SensorType.KINECTSDK2) ? new FubiUtils.StreamOptions() : new FubiUtils.StreamOptions(-1, -1, -1);
            var depthOptions = new FubiUtils.StreamOptions();
            var invalidOptions = new FubiUtils.StreamOptions(-1);
            lock (LockFubiUpdate)
            {
                if (!Fubi.init(new FubiUtils.SensorOptions(depthOptions, rgbOptions, irOptions, m_selectedSensor),
                        (FubiUtils.FilterOptions)filterOptions))
                {
                    m_selectedSensor = FubiUtils.SensorType.NONE;
                    Fubi.init(new FubiUtils.SensorOptions(depthOptions, invalidOptions, invalidOptions, m_selectedSensor),
                        (FubiUtils.FilterOptions)filterOptions);
                    Dispatcher.BeginInvoke(new TwoStringDelegate(showWarnMsg),
                        new object[]
						{
							"Error starting sensor! \nDid you connect the sensor and install the correct driver? \nTry selecting a different sensor.",
							"Error starting sensor"
						});
                }

                if (m_selectedFingerSensor != FubiUtils.FingerSensorType.NONE)
                {
                    float xOffset = 0, yOffset = 0, zOffset = 0;
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        xOffset = (float)xOffsetControl.Value;
                        yOffset = (float)yOffsetControl.Value;
                        zOffset = (float)zOffsetControl.Value;
                    }));
                    if (!Fubi.initFingerSensor(m_selectedFingerSensor, xOffset, yOffset, zOffset))
                    {
                        m_selectedFingerSensor = FubiUtils.FingerSensorType.NONE;
                        Dispatcher.BeginInvoke(new TwoStringDelegate(showWarnMsg),
                            new object[]
							{
								"Error starting finger sensor! \nDid you connect the sensor and install the correct driver?",
								"Error starting finger sensor"
							});
                    }
                }

                // All known combination recognizers will be started automatically for new users
                Fubi.setAutoStartCombinationRecognition(true);

                // Load XML with sample mouse control gestures
                if (Fubi.loadRecognizersFromXML("MouseControlRecognizers.xml"))
                {
                    // This requires to update the gesture list used for selecting key/button bindings and for xml generation
                    Dispatcher.BeginInvoke(new NoArgDelegate(refreshGestureList), null);
                    // Now we can load the default bindings using the above recognizers
                    Dispatcher.BeginInvoke(new OneStringDelegate(m_fubiMouseKeyboard.Bindings.loadFromXML), "KeyMouseBindings.xml");
                }
            }

            Fubi.RecognitionStart += new Fubi.RecognitionHandler(recognitionStart);
            Fubi.RecognitionEnd += new Fubi.RecognitionHandler(recognitionEnd);

            m_switchSensor = UpdateCycle.FubiUpdated;
            m_switchFingerSensor = UpdateCycle.FubiUpdated;
            Dispatcher.BeginInvoke(new NoArgDelegate(disableLoadingCircle), null);

            DispatcherOperation currentOp = null;
            while (m_running)
            {
                var bufferInvalid = s_buffer == null;

                lock (LockFubiUpdate)
                {
                    // Check for update events
                    if (m_clearRecognizers == UpdateCycle.Triggered)
                    {
                        Fubi.clearUserDefinedRecognizers();
                        m_clearRecognizers = UpdateCycle.FubiUpdated;
                    }

                    if (m_resetTracking == UpdateCycle.Triggered)
                    {
                        Fubi.resetTracking();
                        m_resetTracking = UpdateCycle.UpToDate;
                    }

                    if (m_switchSensor == UpdateCycle.Triggered)
                    {
                        Dispatcher.BeginInvoke(new NoArgDelegate(enableLoadingCircle), null);
                        rgbOptions = (m_irStreamActive && m_selectedSensor != FubiUtils.SensorType.KINECTSDK2) ? new FubiUtils.StreamOptions(-1, -1, -1) : new FubiUtils.StreamOptions();
                        irOptions = (m_irStreamActive || m_selectedSensor == FubiUtils.SensorType.KINECTSDK2) ? new FubiUtils.StreamOptions() : new FubiUtils.StreamOptions(-1, -1, -1);
                        if (
                            !Fubi.switchSensor(new FubiUtils.SensorOptions(new FubiUtils.StreamOptions(), rgbOptions, irOptions,
                                m_selectedSensor, FubiUtils.SkeletonProfile.ALL, true, m_registerStreams)))
                        {
                            m_selectedSensor = FubiUtils.SensorType.NONE;
                            Dispatcher.BeginInvoke(new TwoStringDelegate(showWarnMsg),
                                new object[]
								{
									"Error starting sensor! \nDid you connect the sensor and install the correct driver? \nTry selecting a different sensor.",
									"Error starting sensor"
								});
                        }
                        Dispatcher.BeginInvoke(new NoArgDelegate(disableLoadingCircle), null);
                        m_switchSensor = UpdateCycle.FubiUpdated;
                        bufferInvalid = true;
                    }

                    if (m_switchFingerSensor == UpdateCycle.Triggered)
                    {
                        Dispatcher.BeginInvoke(new NoArgDelegate(enableLoadingCircle), null);
                        float xOffset = 0, yOffset = 0, zOffset = 0;
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            xOffset = (float)xOffsetControl.Value;
                            yOffset = (float)yOffsetControl.Value;
                            zOffset = (float)zOffsetControl.Value;
                        }));
                        if (!Fubi.initFingerSensor(m_selectedFingerSensor, xOffset, yOffset, zOffset))
                        {
                            m_selectedFingerSensor = FubiUtils.FingerSensorType.NONE;
                            Dispatcher.BeginInvoke(new TwoStringDelegate(showWarnMsg),
                                new object[]
								{
									"Error starting finger sensor! \nDid you connect the sensor and install the correct driver?",
									"Error starting finger sensor"
								});
                        }
                        Dispatcher.BeginInvoke(new NoArgDelegate(disableLoadingCircle), null);
                        m_switchFingerSensor = UpdateCycle.FubiUpdated;
                        bufferInvalid = true;
                    }

                    // Now update the sensors
                    Fubi.updateSensor();

                    if (!bufferInvalid) // GUI first needs to resize the buffer
                    {
                        // And get the next image
                        if (m_selectedImageType == FubiUtils.ImageType.Blank)
                        {
                            // Special case: reset the array, as this one only adds the tracking info
                            for (var i = 0; i < (int)m_numChannels * m_width * m_height; ++i)
                                s_buffer[i] = 0;
                        }
                        Fubi.getImage(s_buffer, m_selectedImageType, m_numChannels, FubiUtils.ImageDepth.D8, m_renderOptions,
                            m_jointsToRender, m_selectedDepthMod);
                    }

                    if (s_fingerSensorBuffer != null)
                    {
                        Fubi.getImage(s_fingerSensorBuffer, m_fingerSensorImageIndex, FubiUtils.ImageNumChannels.C1, FubiUtils.ImageDepth.D8);
                    }
                }

                // And trigger a GUI update event
                currentOp = Dispatcher.BeginInvoke(new NoArgDelegate(updateGUI), null);
                // Wait for the GUI update to finish
                while (currentOp.Status != DispatcherOperationStatus.Completed
                    && currentOp.Status != DispatcherOperationStatus.Aborted)
                {
                    Thread.Sleep(5);
                }
            }
            // Wait for the last GUI update to really have finished
            while (currentOp != null && currentOp.Status != DispatcherOperationStatus.Completed
                    && currentOp.Status != DispatcherOperationStatus.Aborted)
            {
                Thread.Sleep(2);
            }
            // Now we can release Fubi safely
            Fubi.release();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            m_running = false;

            m_kListener.Dispose();

            m_fubiThread.Join(2000);

            Dispatcher.InvokeShutdown();
        }

        private void enableLoadingCircle()
        {
            loadingCircle1.IsEnabled = true;
        }

        private void disableLoadingCircle()
        {
            loadingCircle1.IsEnabled = false;
        }

        private void updateGUI()
        {
            if (m_switchSensor == UpdateCycle.Triggered || m_switchFingerSensor == UpdateCycle.Triggered)
            {
                // A button press has triggered a sensor switch in between, so we don't need to  update the GUI, but first the sensor has to be switched
            }
            else
            {
                //#ifdef ADD_RP_2015
                if (recordingStarted)
                {
                    Fubi.stopPlayingRecordedSkeletonData();
                    playbackSlider.Value = playbackSlider.StartValue;
                    // Stop button required to stop the recording session
                    stopButton.IsEnabled = true;
                    openRecordingButton.IsEnabled = pauseButton.IsEnabled = playButton.IsEnabled = trimButton.IsEnabled = recordButton.IsEnabled = false;
                    aviFile.pause = false;
                    Settings.Default.LastRecordingFilePath = recordSkeletonFileName;
                    Settings.Default.Save();
                    recordingStarted = false;
                }
                //#endif 

                // Update cycle handling
                if (m_clearRecognizers == UpdateCycle.FubiUpdated)
                {
                    if (Fubi.getNumUserDefinedCombinationRecognizers() == 0 && Fubi.getNumUserDefinedRecognizers() == 0)
                    {
                        button3.IsEnabled = false;
                    }
                    // Update gesture list used for the key/button bindings and for XML generation
                    refreshGestureList();
                    m_clearRecognizers = UpdateCycle.UpToDate;
                }
                if (m_switchSensor == UpdateCycle.FubiUpdated)
                {
                    float minCutOff, velCutOff, slope;
                    Fubi.getFilterOptions(out minCutOff, out velCutOff, out slope);
                    minCutOffControl.Value = minCutOff;
                    cutOffSlopeControl.Value = slope;

                    button4.IsEnabled = m_selectedSensor == FubiUtils.SensorType.OPENNI1;
                    m_switchSensor = UpdateCycle.UpToDate;
                }
                if (m_switchFingerSensor == UpdateCycle.FubiUpdated)
                {
                    if (Fubi.getCurrentSensorType() == FubiUtils.SensorType.NONE && m_selectedFingerSensor == FubiUtils.FingerSensorType.LEAP)
                    {
                        // For leap we automatically select a blank stream if no other sensor is present
                        var blankName = Enum.GetName(typeof(FubiUtils.ImageType), FubiUtils.ImageType.Blank);
                        imageStreamComboBox.SelectedItem = blankName;
                    }
                    m_switchFingerSensor = UpdateCycle.UpToDate;
                }
                // Update selections
                if (imageStreamComboBox.SelectedItem != null)
                    m_selectedImageType = (FubiUtils.ImageType)Enum.Parse(typeof(FubiUtils.ImageType), imageStreamComboBox.SelectedItem.ToString());
                m_renderOptions = 0;
                if (shapeCheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.Shapes;
                if (skeletonCheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.Skeletons;
                if (userCaptionscheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.UserCaptions;
                if (backgroundCheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.Background;
                if (swapRAndBcheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.SwapRAndB;
                if (fingerShapecheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.FingerShapes;
                if (detailedFaceCheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.DetailedFaceShapes;
                if (bodyMeasuresCheckBox.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.BodyMeasurements;
                if (orientRadioButton.IsChecked == true)
                {
                    if (globalRadioButton.IsChecked == true)
                        m_renderOptions |= (int)FubiUtils.RenderOptions.GlobalOrientCaptions;
                    else
                        m_renderOptions |= (int)FubiUtils.RenderOptions.LocalOrientCaptions;
                }
                else if (posRadioButton.IsChecked == true)
                {
                    if (globalRadioButton.IsChecked == true)
                        m_renderOptions |= (int)FubiUtils.RenderOptions.GlobalPosCaptions;
                    else
                        m_renderOptions |= (int)FubiUtils.RenderOptions.LocalPosCaptions;
                }
                if (filteredRadioButton.IsChecked == true)
                    m_renderOptions |= (int)FubiUtils.RenderOptions.UseFilteredValues;
                m_jointsToRender = 0;
                var query = from r in m_jointsToRenderDt.AsEnumerable()
                            where r.Field<bool>("IsChecked")
                            select new
                            {
                                Name = r["Name"]
                            };
                foreach (var r in query)
                {
                    var selection = (FubiUtils.JointsToRender)Enum.Parse(typeof(FubiUtils.JointsToRender), r.Name.ToString());
                    if (selection != FubiUtils.JointsToRender.ALL_JOINTS)
                        m_jointsToRender |= (int)selection;
                }
                m_selectedDepthMod = (FubiUtils.DepthImageModification)Enum.Parse(typeof(FubiUtils.DepthImageModification), depthModComboBox.SelectedItem.ToString(), true);


                // Display image
                m_width = 0; m_height = 0;
                m_numChannels = FubiUtils.ImageNumChannels.C4;
                if (m_selectedImageType == FubiUtils.ImageType.Color)
                {
                    Fubi.getRgbResolution(out m_width, out m_height);
                    m_numChannels = FubiUtils.ImageNumChannels.C3;
                }
                else if (m_selectedImageType == FubiUtils.ImageType.IR)
                {
                    Fubi.getIRResolution(out m_width, out m_height);
                    m_numChannels = FubiUtils.ImageNumChannels.C3;
                }
                else if (m_selectedImageType == FubiUtils.ImageType.Blank)
                {
                    m_width = 640; m_height = 480;
                    m_numChannels = FubiUtils.ImageNumChannels.C4;
                }
                else
                {
                    Fubi.getDepthResolution(out m_width, out m_height);
                    m_numChannels = FubiUtils.ImageNumChannels.C4;
                }
                if (m_width > 0 && m_height > 0)
                {
                    var wb = image1.Source as WriteableBitmap;
                    var bufferChanged = false;
                    if (wb == null || (Math.Abs(wb.Width - m_width) > Double.Epsilon || Math.Abs(wb.Height - m_height) > Double.Epsilon || wb.Format.BitsPerPixel != (int)m_numChannels * 8))
                    {
                        // Image uninitialized or resoultion changed, so reset the display image and resize the buffer for the next update
                        var format = PixelFormats.Bgra32;
                        if (m_numChannels == FubiUtils.ImageNumChannels.C3)
                            format = PixelFormats.Rgb24;
                        else if (m_numChannels == FubiUtils.ImageNumChannels.C1)
                            format = PixelFormats.Gray8;
                        wb = new WriteableBitmap(m_width, m_height, 0, 0, format, BitmapPalettes.Gray256);
                        s_buffer = new byte[(int)m_numChannels * m_width * m_height];
                        image1.Source = wb;
                        bufferChanged = true;
                    }

                    if (!bufferChanged && !aviFile.pause)
                    {
                        // Buffer hasn't been resized so it should be valid
                        var stride = wb.PixelWidth * (wb.Format.BitsPerPixel / 8);
                        //#ifdef ADD_RP_2015
                        //try
                        //{
                        if (aviFile.saveMode && Fubi.isRecordingSkeletonData())
                            aviFile.saveFrame(s_buffer);
                        if (aviFile.playMode && Fubi.isPlayingSkeletonData())
                        {
                            aviFile.readFrame(s_buffer, playbackSlider.Value);
                        }
                        //}
                        //catch (Exception ex)
                        //{
                        //    MessageBox.Show(ex.ToString());
                        //}

                        //#endif
                        wb.WritePixels(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight), s_buffer, stride, 0);
                    }
                }

                // Similar for finger sensor image
                int fsWidth, fsHeight;
                uint fsNumStreams;
                Fubi.getFingerSensorImageConfig(out fsWidth, out fsHeight, out fsNumStreams);
                fsImageIndex.Maximum = (double)fsNumStreams - 1;
                m_fingerSensorImageIndex = (uint)fsImageIndex.Value;
                if (fsNumStreams > 0 && fingerSensorToggle.IsChecked == true)
                {
                    var wb = fingerSensorImage.Source as WriteableBitmap;
                    var bufferChanged = false;
                    if (wb == null || (Math.Abs(wb.Width - fsWidth) > Double.Epsilon || Math.Abs(wb.Height - fsHeight) > Double.Epsilon))
                    {
                        // Image uninitialized or resoultion changed, so reset the display image and resize the buffer for the next update
                        wb = new WriteableBitmap(fsWidth, fsHeight, 0, 0, PixelFormats.Gray8, BitmapPalettes.Gray256);
                        s_fingerSensorBuffer = new byte[fsWidth * fsHeight];
                        fingerSensorImage.Source = wb;
                        bufferChanged = true;
                    }
                    if (!bufferChanged)
                    {
                        // Buffer hasn't been resized so it should be valid
                        var stride = wb.PixelWidth * (wb.Format.BitsPerPixel / 8);
                        wb.WritePixels(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight), s_fingerSensorBuffer, stride, 0);
                        fingerSensorImage.Visibility = Visibility.Visible;
                    }
                }
                else
                    fingerSensorImage.Visibility = Visibility.Hidden;

                //#ifdef ADD_RP_2015
                var numUsers = Fubi.getNumUsers();
                if (lastNumUsers != numUsers)
                    Console.WriteLine("Num users:" + numUsers.ToString());
                lastNumUsers = numUsers;
                //#endif

                // Check user defined combinations for correction hints displayed in the stats window
                if (m_statsWindow != null)
                {
                    for (uint i = 0; i < numUsers + 1; i++)
                    {
                        var id = (i == numUsers && Fubi.isPlayingSkeletonData()) ? FubiUtils.PlaybackUserID : Fubi.getUserID(i);
                        if (id > 0)
                        {
                            for (uint pc = 0; pc < Fubi.getNumUserDefinedCombinationRecognizers(); ++pc)
                            {
                                var name = Fubi.getUserDefinedCombinationRecognizerName(pc);
                                FubiUtils.RecognitionCorrectionHint correctionHint;
                                var res = Fubi.getCombinationRecognitionProgressOn(name, id, out correctionHint);
                                if (res != FubiUtils.RecognitionResult.RECOGNIZED && correctionHint.m_joint != FubiUtils.SkeletonJoint.NUM_JOINTS)
                                {
                                    var msg = FubiUtils.createCorrectionHintMsg(correctionHint);
                                    if (msg.Length > 0)
                                    {
                                        if (msg.EndsWith("\n"))
                                            msg = msg.Remove(msg.Length - 1);
                                        if (m_statsWindow.Hints.ContainsKey(id) == false)
                                            m_statsWindow.Hints[id] = new Dictionary<string, string>();
                                        m_statsWindow.Hints[id][name] = msg;
                                    }
                                }
                            }
                        }
                    }
                }

                //Now for all hands
                var numhands = Fubi.getNumHands();
                for (uint i = 0; i < numhands + 1; i++)
                {
                    var id = (i == numhands && Fubi.isPlayingSkeletonData()) ? FubiUtils.PlaybackHandID : Fubi.getHandID(i);
                    if (id > 0)
                    {
                        var changedSomething = false;
                        // Print gestures
                        if (checkBox1.IsChecked == true || m_enableKeyMouseBinding)
                        {
                            if (!m_currentFingerGestures.ContainsKey(id))
                                m_currentFingerGestures[id] = new Dictionary<uint, bool>();

                            // Only user defined gestures
                            for (uint p = 0; p < Fubi.getNumUserDefinedRecognizers(); ++p)
                            {
                                var pName = Fubi.getUserDefinedRecognizerName(p);
                                if (Fubi.recognizeGestureOnHand(pName, id) == FubiUtils.RecognitionResult.RECOGNIZED)
                                {
                                    // Gesture recognized
                                    if (!m_currentFingerGestures[id].ContainsKey(p) || !m_currentFingerGestures[id][p])
                                    {
                                        // Gesture start
                                        if (checkBox1.IsChecked == true)
                                            textBox1.Text += "Hand" + id + ": START OF " + pName + " -->\n";
                                        m_currentFingerGestures[id][p] = true;
                                        changedSomething = true;
                                        if (m_enableKeyMouseBinding)
                                            m_fubiMouseKeyboard.applyBinding(Fubi.getUserDefinedRecognizerName(p), true);
                                    }
                                }
                                else if (m_currentFingerGestures[id].ContainsKey(p) && m_currentFingerGestures[id][p])
                                {
                                    // Gesture end
                                    if (checkBox1.IsChecked == true)
                                        textBox1.Text += "Hand" + id + ": --> END OF " + pName + "\n";
                                    m_currentFingerGestures[id][p] = false;
                                    changedSomething = true;
                                    if (m_enableKeyMouseBinding)
                                        m_fubiMouseKeyboard.applyBinding(Fubi.getUserDefinedRecognizerName(p), false);
                                }
                            }
                            if (changedSomething)
                                textBox1.ScrollToEnd();
                        }

                        // Print combinations
                        for (uint pc = 0; pc < Fubi.getNumUserDefinedCombinationRecognizers(); ++pc)
                        {
                            // User defined gestures
                            var name = Fubi.getUserDefinedCombinationRecognizerName(pc);
                            FubiUtils.RecognitionCorrectionHint correctionHint;
                            var res = Fubi.getCombinationRecognitionProgressOnHand(name, id, out correctionHint);
                            if (res == FubiUtils.RecognitionResult.RECOGNIZED)
                            {
                                // Combination recognized
                                if (checkBox2.IsChecked == true)
                                    textBox2.Text += "Hand" + id + ": " + name + "\n";
                                if (m_statsWindow != null)
                                {
                                    if (m_statsWindow.HandRecognitions.ContainsKey(id) == false)
                                        m_statsWindow.HandRecognitions[id] = new Dictionary<string, double>();
                                    m_statsWindow.HandRecognitions[id][name] = Fubi.getCurrentTime();
                                }
                                if (m_enableKeyMouseBinding)
                                {
                                    m_fubiMouseKeyboard.applyBinding(Fubi.getUserDefinedCombinationRecognizerName(pc), true);
                                    m_fubiMouseKeyboard.applyBinding(Fubi.getUserDefinedCombinationRecognizerName(pc), false);
                                }
                            }
                            else
                            {
                                if (res == FubiUtils.RecognitionResult.NOT_RECOGNIZED && m_statsWindow != null && correctionHint.m_joint != FubiUtils.SkeletonJoint.NUM_JOINTS)
                                {
                                    var msg = FubiUtils.createCorrectionHintMsg(correctionHint);

                                    if (msg.Length > 0)
                                    {
                                        if (m_statsWindow.HandHints.ContainsKey(id) == false)
                                            m_statsWindow.HandHints[id] = new Dictionary<string, string>();
                                        m_statsWindow.HandHints[id][name] = msg;
                                    }
                                }
                                Fubi.enableCombinationRecognitionHand(name, id, true);
                            }
                        }

                        if (checkBox2.IsChecked == true)
                            textBox2.ScrollToEnd();
                    }
                }

                // Display user count
                label1.Content = "User Count: " + Fubi.getNumUsers() + " - Hand Count: " + Fubi.getNumHands();

                // Mouse control
                var closestId = Fubi.getClosestUserID();
                if (closestId > 0)
                {
                    if (m_controlMouse)
                    {
                        float x, y;
                        m_fubiMouseKeyboard.applyHandPositionToMouse(closestId, out x, out y, leftHandRadioButton.IsChecked == true);
                        mouseCoordLabel.Content = "X:" + x + " Y:" + y;
                    }

                    if (mouseEmuStartWithWavingCheckBox.IsChecked == true)
                    {
                        var activationGesture = (leftHandRadioButton.IsChecked == true) ?
                            FubiPredefinedGestures.Combinations.WAVE_LEFT_HAND_OVER_SHOULDER :
                            FubiPredefinedGestures.Combinations.WAVE_RIGHT_HAND_OVER_SHOULDER;
                        if (Fubi.getCombinationRecognitionProgressOn(activationGesture, closestId, false) == FubiUtils.RecognitionResult.RECOGNIZED)
                        {
                            if (m_controlMouse)
                                stopMouseEmulation();
                            else
                                startMouseEmulation();
                        }
                        else
                        {
                            Fubi.enableCombinationRecognition(activationGesture, closestId, true);
                        }
                    }
                }

                int currentFrame = 0;
                bool isPaused = false;
                if (Fubi.isPlayingSkeletonData(ref currentFrame, ref isPaused))
                {
                    if (!isPaused)
                        playbackSlider.Value = currentFrame;
                }
                else if (pauseButton.IsEnabled) // Was playing in last frame, so we stop now
                {
                    pauseButton.IsEnabled = stopButton.IsEnabled = false;
                    playButton.IsEnabled = trimButton.IsEnabled = recordButton.IsEnabled = true;
                    playbackSlider.Value = playbackSlider.EndValue;
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (m_openRecDlg == null)
            {
                m_openRecDlg = new OpenFileDialog
                {
                    FileName = Path.GetFileName(Settings.Default.LastRecognizerXMLPath),
                    InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Settings.Default.LastRecognizerXMLPath)),
                    DefaultExt = ".xml",
                    Filter = "XML documents (.xml)|*.xml"
                };
            }

            // Show open file dialog box 
            var result = m_openRecDlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true && m_openRecDlg.FileName != null)
            {
                button1.IsEnabled = false;
                Task.Factory.StartNew(() =>
                {
                    bool updated;
                    lock (LockFubiUpdate)
                    {
                        // Open document 
                        Dispatcher.Invoke(new NoArgDelegate(enableLoadingCircle), null);
                        updated = Fubi.loadRecognizersFromXML(m_openRecDlg.FileName);
                        Dispatcher.Invoke(new NoArgDelegate(disableLoadingCircle), null);
                    }

                    Dispatcher.Invoke((Action)(() =>
                    {
                        if (updated)
                        {
                            Settings.Default.LastRecognizerXMLPath = m_openRecDlg.FileName;
                            Settings.Default.Save();
                        }

                        // Update gesture list used for the key/button bindings and for XML generation
                        refreshGestureList();

                        // Enable clear button if we have loaded some recognizers
                        button3.IsEnabled = (Fubi.getNumUserDefinedCombinationRecognizers() > 0 || Fubi.getNumUserDefinedRecognizers() > 0);
                        button1.IsEnabled = true;
                    }));
                });
            }
        }

        private void cursorControlButton_Click(object sender, RoutedEventArgs e)
        {
            // Enable/Disable mouse emulation
            if (m_controlMouse)
            {
                stopMouseEmulation();
            }
            else
            {
                startMouseEmulation();
            }
        }

        private void keyPressedHandler(object sender, RawKeyEventArgs args)
        {
            // ESC stops mouse emulation
            if (args.Key == Key.Escape)
            {
                Dispatcher.BeginInvoke(new NoArgDelegate(stopMouseEmulation), null);



                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (m_cancelXmlGenToken != null)
                    {
                        m_cancelXmlGenToken.Cancel();
                        menuTabCtrl.IsEnabled = true;
                        m_cancelXmlGenToken = null;
                    }
                }));
            }
        }

        private void startMouseEmulation()
        {
            m_controlMouse = true;
            cursorControlButton.Content = "Disable (ESC)";
            mouseCoordLabel.Content = "X:0 Y:0";
            m_fubiMouseKeyboard.autoCalibrateMapping(leftHandRadioButton.IsChecked == true);
        }

        private void stopMouseEmulation()
        {
            m_controlMouse = false;
            cursorControlButton.Content = "Enable";
            mouseCoordLabel.Content = "";
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            // Clear user defined recognizers
            m_clearRecognizers = UpdateCycle.Triggered;
        }

        private void sensorSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_selectedSensor = (FubiUtils.SensorType)Enum.Parse(typeof(FubiUtils.SensorType), sensorSelectionComboBox.SelectedItem.ToString());
            m_switchSensor = UpdateCycle.Triggered;
        }

        private void Expander_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                var exp = (Expander)sender;
                var rowIndex = Grid.GetRow(exp);
                var row = mainGrid.RowDefinitions[rowIndex];

                if (exp.IsExpanded)
                {
                    row.Height = m_rowHeights[rowIndex];
                }
                else
                {
                    m_rowHeights[rowIndex] = row.Height;
                    row.Height = GridLength.Auto;
                }

                updateExpanderConstrains();
            }
        }

        private void updateExpanderConstrains()
        {
            double w = 180;
            double h = 159;
            var menuRowIndex = Grid.GetRow(menuExpander);
            var menuRow = mainGrid.RowDefinitions[menuRowIndex];
            var logRowIndex = Grid.GetRow(logExpander);
            var logRow = mainGrid.RowDefinitions[logRowIndex];

            if (menuExpander.IsExpanded)
            {
                h += menuTabCtrl.MinHeight + 25 + 3;
                w = 660;
                menuRow.MinHeight = menuTabCtrl.MinHeight + 25;
                menuSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                h += 25 + 3;
                menuRow.MinHeight = 25;
                menuSplitter.Visibility = Visibility.Collapsed;
            }

            if (logExpander.IsExpanded)
            {
                h += logGrid.MinHeight + 25 + 3;
                w = 660;
                logRow.MinHeight = logGrid.MinHeight + 25;
                logSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                h += 25 + 3;
                logRow.MinHeight = 25;
                logSplitter.Visibility = Visibility.Collapsed;
            }

            MinWidth = w;
            MinHeight = h;
        }

        private void imageStreamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                var newType = (FubiUtils.ImageType)Enum.Parse(typeof(FubiUtils.ImageType), imageStreamComboBox.SelectedItem.ToString());

                // Check if there was a switch between rgb and ir
                if (m_selectedSensor != FubiUtils.SensorType.NONE && m_selectedSensor != FubiUtils.SensorType.KINECTSDK2)
                {
                    if (newType == FubiUtils.ImageType.Color && m_irStreamActive)
                    {
                        m_switchSensor = UpdateCycle.Triggered;
                        m_irStreamActive = false;
                    }
                    else if (newType == FubiUtils.ImageType.IR && !m_irStreamActive)
                    {
                        m_switchSensor = UpdateCycle.Triggered;
                        m_irStreamActive = true;
                    }
                }
                else
                {
                    m_irStreamActive = newType == FubiUtils.ImageType.IR;
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            if ((FubiUtils.JointsToRender)Enum.Parse(typeof(FubiUtils.JointsToRender), box.Tag.ToString()) == FubiUtils.JointsToRender.ALL_JOINTS)
            {
                foreach (var d in m_jointsToRenderDt.AsEnumerable())
                {
                    d.SetField("IsChecked", true);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var box = (CheckBox)sender;
            if ((FubiUtils.JointsToRender)Enum.Parse(typeof(FubiUtils.JointsToRender), box.Tag.ToString()) == FubiUtils.JointsToRender.ALL_JOINTS)
            {
                foreach (var d in m_jointsToRenderDt.AsEnumerable())
                {
                    d.SetField("IsChecked", false);
                }
            }
        }

        private void jointsToRenderCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            jointsToRenderCb.SelectedItem = null;
        }

        private void openRecStats_click(object sender, RoutedEventArgs e)
        {
            if (m_statsWindow == null)
            {
                m_statsWindow = new RecognizerStatsWindow { Owner = this };
                m_statsWindow.Closed += statsWindow_Closed;
                m_statsWindow.Left = Left + Width;
                m_statsWindow.Top = Top;
                m_statsWindow.Show();
            }
            else
                m_statsWindow.Close();
        }

        private void registerStreams_checkBox_Changed(object sender, RoutedEventArgs e)
        {
            m_registerStreams = registerStreams_CheckBox.IsChecked == true;
            m_switchSensor = UpdateCycle.Triggered;
        }

        private void fingerSensorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_selectedFingerSensor = (FubiUtils.FingerSensorType)Enum.Parse(typeof(FubiUtils.FingerSensorType), fingerSensorComboBox.SelectedItem.ToString());
            m_switchFingerSensor = UpdateCycle.Triggered;
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            m_resetTracking = UpdateCycle.Triggered;
        }

        private void statsWindow_Closed(object sender, EventArgs e)
        {
            m_statsWindow = null;
            if (IsLoaded)
                openRecStats.IsChecked = false;
        }

        // Usually you should not call Fubi functions directly from gui events, but the following should should be no problem :)
        private void minCutOffControl_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                lock (LockFubiUpdate)
                {
                    float minCutOff, velCutOff, slope;
                    Fubi.getFilterOptions(out minCutOff, out velCutOff, out slope);
                    Fubi.setFilterOptions((float)minCutOffControl.Value, velCutOff, slope);
                }
            }
        }
        private void cutOffSlopeControl_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                lock (LockFubiUpdate)
                {
                    float minCutOff, velCutOff, slope;
                    Fubi.getFilterOptions(out minCutOff, out velCutOff, out slope);
                    Fubi.setFilterOptions(minCutOff, velCutOff, (float)cutOffSlopeControl.Value);
                }
            }
        }
        private void fSensorOffset_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                lock (LockFubiUpdate)
                {
                    Fubi.setFingerSensorOffsetPosition((float)xOffsetControl.Value, (float)yOffsetControl.Value,
                        (float)zOffsetControl.Value);
                }
            }
        }

        private void KeyMouseBindingTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // The text box grabs all input
            e.Handled = true;

            // Fetch the actual shortcut key.
            var key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Ignore modifier keys.
            if (key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            // First add modifiers
            var shortcutText = new StringBuilder();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                shortcutText.Append("Ctrl+");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                shortcutText.Append("Shift+");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                shortcutText.Append("Alt+");
            }

            // Then the actual key
            shortcutText.Append(key.ToString());

            // Set the texbox text
            ((TextBox)sender).Text = shortcutText.ToString();

            // And move the focus to the next grid element
            var request = new TraversalRequest(FocusNavigationDirection.Next) { Wrapped = true };
            ((TextBox)sender).MoveFocus(request);
        }

        private void KeyMouseBindingTextbox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var box = (TextBox)sender;

            if (box.IsFocused)
            {
                // The text box grabs all input
                e.Handled = true;

                // First add modifiers
                var shortcutText = new StringBuilder();
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                {
                    shortcutText.Append("Ctrl+");
                }
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                {
                    shortcutText.Append("Shift+");
                }
                if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                {
                    shortcutText.Append("Alt+");
                }

                // Add the mouse button
                var button = "LMB";
                switch (e.ChangedButton)
                {
                    case MouseButton.Right:
                        button = "RMB";
                        break;
                    case MouseButton.Middle:
                        button = "MMB";
                        break;
                    case MouseButton.XButton1:
                        button = "X1MB";
                        break;
                    case MouseButton.XButton2:
                        button = "X2MB";
                        break;
                }
                shortcutText.Append(button);

                // Set the text box text
                ((TextBox)sender).Text = shortcutText.ToString();

                // And move the focus to the next grid element
                var request = new TraversalRequest(FocusNavigationDirection.Next) { Wrapped = true };
                ((TextBox)sender).MoveFocus(request);
            }


        }

        private void KeyMouseBindingsCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cell = (DataGridCell)sender;
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                    cell.Focus();
                var dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        var row = FindVisualParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                            row.IsSelected = true;
                    }
                }
            }
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

        private void bindingButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_enableKeyMouseBinding)
            {
                m_enableKeyMouseBinding = false;
                bindingButton.Content = "Enable";
            }
            else
            {
                m_enableKeyMouseBinding = true;
                bindingButton.Content = "Disable";
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
            {
                var imgRowIndex = Grid.GetRow(image1);
                var imgRow = mainGrid.RowDefinitions[imgRowIndex];
                // Hack to fix the resize problem of the grid with splitters and expanders
                var heightDiff = mainGrid.ActualHeight - (ActualHeight - 39);
                if (heightDiff > 0.5 && imgRow.Height.Value > 120 + heightDiff)
                {
                    // We have to much difference so decrease the middle row size (image)
                    mainGrid.RowDefinitions[2].Height = new GridLength(imgRow.Height.Value - heightDiff, GridUnitType.Star);
                    // And directly render again to avoid jerking
                    Measure(new Size(Width, Height));
                    Arrange(new Rect(DesiredSize));
                }

            }
        }

        private void loadBindingButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                FileName = Path.GetFileName(Settings.Default.LastBindingXMLPath),
                InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Settings.Default.LastBindingXMLPath)),
                DefaultExt = ".xml",
                Filter = "XML documents (.xml)|*.xml"
            };
            var result = dialog.ShowDialog();
            if (result == true && dialog.FileName != null)
            {
                try
                {
                    m_fubiMouseKeyboard.Bindings.loadFromXML(dialog.FileName);
                    Settings.Default.LastBindingXMLPath = dialog.FileName;
                    Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    showWarnMsg(ex.Message, "Error loading file");
                }
            }
        }

        private void saveBindingButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                FileName = Path.GetFileName(Settings.Default.LastBindingXMLPath),
                InitialDirectory = Path.GetDirectoryName(Settings.Default.LastBindingXMLPath),
                DefaultExt = ".xml",
                AddExtension = true,
                Filter = "XML documents (.xml)|*.xml"
            };
            var result = dialog.ShowDialog();
            if (result == true && dialog.FileName != null)
            {
                try
                {
                    m_fubiMouseKeyboard.Bindings.saveToXML(dialog.FileName);
                    Settings.Default.LastBindingXMLPath = dialog.FileName;
                    Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    showWarnMsg(ex.Message, "Error saving file");
                }
            }
        }

        private void clearBindingButton_Click(object sender, RoutedEventArgs e)
        {
            m_fubiMouseKeyboard.Bindings.Clear();
        }

        private void MenuTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (menuTabCtrl.SelectedItem != null && menuTabCtrl.SelectedItem == xmlGeneratorTab)
            {
                if (m_xmlGenOptionsWnd == null)
                {
                    m_xmlGenOptionsWnd = new XMLGenRecognizerOptionsWindow(getSelectedXmlGenRecType()) { Owner = this };
                    m_xmlGenOptionsWnd.OptionsChanged += xmlGenOptionsChanged;
                    xmlGenUseHand.IsChecked = false;
                }
                if (m_xmlGenCombOptionsWnd == null)
                {
                    m_xmlGenCombOptionsWnd = new XMLGenCombinationOptionsWindow { Owner = this };
                    m_xmlGenCombOptionsWnd.OptionsChanged += xmlGenOptionsChanged;
                    m_xmlGenCombOptionsWnd.checkOptions();
                }
            }
            else
            {
                if (m_xmlGenOptionsWnd != null)
                    m_xmlGenOptionsWnd.Close();
                if (m_xmlGenCombOptionsWnd != null)
                    m_xmlGenCombOptionsWnd.Close();
            }
        }

        public XMLGenerator.RecognizerType getSelectedXmlGenRecType()
        {
            if (xmlGenRecTypeComboBox.SelectedItem != null)
                return (XMLGenerator.RecognizerType)Enum.Parse(typeof(XMLGenerator.RecognizerType), xmlGenRecTypeComboBox.SelectedItem.ToString());
            return XMLGenerator.RecognizerType.JointRelation;
        }

        private void xmlGenRecOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the corresponding options window for basic recognizers and combinations
            var recType = getSelectedXmlGenRecType();
            if (recType == XMLGenerator.RecognizerType.Combination)
            {
                m_xmlGenCombOptionsWnd.Visibility = m_xmlGenCombOptionsWnd.IsVisible == false ? Visibility.Visible : Visibility.Hidden;
            }
            else
            {
                m_xmlGenOptionsWnd.Visibility = m_xmlGenOptionsWnd.IsVisible == false ? Visibility.Visible : Visibility.Hidden;
            }
        }

        //delays the recording to the seconds in the start combobox and 
        //records for a duration of the seconds selected in the duration combobox
        private void xmlGenStartRecording_Click(object sender, RoutedEventArgs e)
        {
            // Set general options
            m_xmlGenerator.Type = getSelectedXmlGenRecType();
            m_xmlGenerator.RecognizerName = xmlGenRecName.Text;
            m_xmlGenerator.UseHand = xmlGenUseHand.IsChecked == true;
            // Specific options
            if (m_xmlGenerator.Type == XMLGenerator.RecognizerType.Combination)
            {
                m_xmlGenCombOptionsWnd.getOptions(ref m_xmlGenerator.CombOptions);
            }
            else
            {
                m_xmlGenOptionsWnd.getOptions(ref m_xmlGenerator.RecOptions);
            }

            if (xmlGenUseRecording.IsChecked == true
                && m_xmlGenerator.Type != XMLGenerator.RecognizerType.TemplateRecording
                && (m_xmlGenerator.Type != XMLGenerator.RecognizerType.Combination || m_xmlGenerator.CombOptions.TrainType != XMLGenerator.CombinationTrainingType.None))
            {
                lock (LockFubiUpdate)
                {
                    Fubi.stopPlayingRecordedSkeletonData();
                    m_xmlGenerator.Start = 0;
                    m_xmlGenerator.Duration = Fubi.getPlaybackDuration();
                    m_xmlGenerator.StartedWithPlayback = true;
                    Fubi.startPlayingSkeletonData();
                }
                if (Fubi.isPlayingSkeletonData())
                {
                    pauseButton.IsEnabled = stopButton.IsEnabled = true;
                    playButton.IsEnabled = trimButton.IsEnabled = recordButton.IsEnabled = false;
                }
                else
                {
                    showWarnMsg("Unable to start playing skeleton data!\nHave you loaded a recording file?", "Error");
                    return;
                }
            }
            else
            {
                m_xmlGenerator.Start = xmlGenStartingCountdownControl.Value;
                m_xmlGenerator.Duration = xmlGenRecDurationControl.Value;
                m_xmlGenerator.StartedWithPlayback = false;
            }
            m_xmlGenerator.RecOptions.PlaybackStart = playbackSlider.StartValue;
            m_xmlGenerator.RecOptions.PlaybackEnd = playbackSlider.EndValue;

            // Deactivate whole tab menu so no changes occur during recording
            menuTabCtrl.IsEnabled = false;
            // Now start recording
            m_cancelXmlGenToken = new CancellationTokenSource();
            Task.Factory.StartNew(() => m_xmlGenerator.generateXML(this, m_cancelXmlGenToken.Token));
        }

        private void xmlGenOptionsChanged(object sender, XMLGenerator.RecognizerOptionsChangedArgs args)
        {
            if (args.OptionsValid && xmlGenRecName.Text.Trim() != "")
            {
                if (getSelectedXmlGenRecType() == XMLGenerator.RecognizerType.TemplateRecording
                    && playbackSlider.Maximum == 0) // Maximum == 0 means no file loaded
                    xmlGenStartButton.IsEnabled = false;
                else
                    xmlGenStartButton.IsEnabled = true;
            }
            else
                xmlGenStartButton.IsEnabled = false;
        }

        private void recognizerType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (m_xmlGenOptionsWnd != null && m_xmlGenCombOptionsWnd != null)
            {
                xmlGenOptionsButton.IsEnabled = true;
                var type = getSelectedXmlGenRecType();
                if (type == XMLGenerator.RecognizerType.Combination)
                {
                    if (m_xmlGenOptionsWnd.IsVisible)
                    {
                        m_xmlGenOptionsWnd.Visibility = Visibility.Hidden;
                        xmlGenStartButton.IsEnabled = false;
                        m_xmlGenCombOptionsWnd.Visibility = Visibility.Visible;
                        m_xmlGenCombOptionsWnd.checkOptions();
                    }
                }
                else
                {
                    if (m_xmlGenCombOptionsWnd.IsVisible)
                    {
                        m_xmlGenCombOptionsWnd.Visibility = Visibility.Hidden;
                        m_xmlGenOptionsWnd.Visibility = Visibility.Visible;
                        m_xmlGenOptionsWnd.checkSelections();
                    }
                    m_xmlGenOptionsWnd.Type = type;
                }
            }
        }

        private void xmlGenStartingCountdownControl_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (xmlGenCountDownLabel != null)
                xmlGenCountDownLabel.Content = xmlGenStartingCountdownControl.Value.ToString("F2");
        }

        private void xmlGenRecName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (getSelectedXmlGenRecType() == XMLGenerator.RecognizerType.Combination)
                m_xmlGenCombOptionsWnd.checkOptions();
            else
                m_xmlGenOptionsWnd.checkSelections();
        }

        static public Control findControl(string name, DependencyObject parent)
        {
            if (parent != null)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(parent))
                {
                    // First check if we found the control
                    var control = child as Control;
                    if (control == null || control.Name.ToLower() != name.ToLower())
                        // Now look at the child's children recursively
                        control = findControl(name, child as DependencyObject);
                    if (control != null && control.Name.ToLower() == name.ToLower())
                        return control;
                }
            }
            return null;
        }

        static public IEnumerable<T> FindLogicalChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent != null)
            {
                foreach (var child in LogicalTreeHelper.GetChildren(parent))
                {
                    if (child is T)
                        yield return (T)child;
                    foreach (var childOfChild in FindLogicalChildren<T>(child as DependencyObject))
                        yield return childOfChild;
                }
            }
        }

        static public void setControlValue(Control ctrl, string value)
        {
            var down = ctrl as NumericUpDown;
            if (down != null)
            {
                down.Value = Double.Parse(value);
            }
            else
            {
                var box = ctrl as CheckBox;
                if (box != null)
                {
                    box.IsChecked = Boolean.Parse(value);
                }
                else
                {
                    var but = ctrl as RadioButton;
                    if (but != null)
                    {
                        but.IsChecked = Boolean.Parse(value);
                    }
                    else
                    {
                        var comboBox = ctrl as ComboBox;
                        if (comboBox != null)
                        {
                            comboBox.SelectedItem = value;
                        }
                    }
                }
            }
        }

        static public string getControlValue(Control ctrl)
        {
            var down = ctrl as NumericUpDown;
            if (down != null)
            {
                return down.Value.ToString();
            }
            var box = ctrl as CheckBox;
            if (box != null)
            {
                return box.IsChecked.ToString();
            }
            var but = ctrl as RadioButton;
            if (but != null)
            {
                return but.IsChecked.ToString();
            }
            var comboBox = ctrl as ComboBox;
            if (comboBox != null)
            {
                return comboBox.SelectedItem.ToString();
            }
            return null;
        }

        public static String GetRelativePath(String fromPath, String toPath, bool replacePathSeparator = false)
        {
            if (String.IsNullOrEmpty(fromPath) || String.IsNullOrEmpty(toPath))
                return toPath;

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
                return toPath;

            // The actual conversion
            var relativePath = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());

            if (replacePathSeparator && toUri.Scheme.ToUpperInvariant() == "FILE")
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return relativePath;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Settings.Default.ControlValues == null)
                Settings.Default.ControlValues = new SerializableStringDictionary();
            // Save control values for the different tabs
            foreach (var cbBox in FindLogicalChildren<ComboBox>(menuTabCtrl))
            {
                if (cbBox.Name != null && cbBox.SelectedItem != null && cbBox != jointsToRenderCb && cbBox != xmlGenRecTypeComboBox)
                    Settings.Default.ControlValues[cbBox.Name] = cbBox.SelectedItem.ToString();
            }
            foreach (var num in FindLogicalChildren<NumericUpDown>(menuTabCtrl))
            {
                if (num.Name != null)
                    Settings.Default.ControlValues[num.Name] = num.Value.ToString();
            }
            foreach (var cBox in FindLogicalChildren<CheckBox>(menuTabCtrl))
            {
                if (cBox.Name != null)
                    Settings.Default.ControlValues[cBox.Name] = cBox.IsChecked.ToString();
            }
            foreach (var rBut in FindLogicalChildren<RadioButton>(menuTabCtrl))
            {
                if (rBut.Name != null)
                    Settings.Default.ControlValues[rBut.Name] = rBut.IsChecked.ToString();
            }
            Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load Control Values from last setting
            if (Settings.Default.ControlValues != null)
            {
                foreach (DictionaryEntry item in Settings.Default.ControlValues)
                {
                    if (item.Value != null)
                    {
                        var ctrl = findControl(item.Key.ToString(), this);
                        if (ctrl != null)
                            setControlValue(ctrl, item.Value.ToString());
                    }
                }
            }

            // Only start the thread after the windows has loaded
            m_fubiThread.Start(new FubiUtils.FilterOptions((float)minCutOffControl.Value, 1.0f, (float)cutOffSlopeControl.Value));
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            //#ifdef ADD_RP_2015
            if (recordImageCheckBox.IsChecked == true)
            {
                if (!aviFile.pause)
                    aviFile.startPlayback();
                else
                    aviFile.pause = false;
            }
            //#endif


            lock (LockFubiUpdate)
            {
                Fubi.startPlayingSkeletonData(repeatButton.IsChecked == true);
            }
            pauseButton.IsEnabled = stopButton.IsEnabled = true;
            playButton.IsEnabled = trimButton.IsEnabled = recordButton.IsEnabled = false;
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            lock (LockFubiUpdate)
            {
                aviFile.stopSave();
                aviFile.stopPlay();
                // Stop button either stops recording or playback
                if (Fubi.isRecordingSkeletonData())
                {

                    Fubi.stopRecordingSkeletonData();
                    stopButton.IsEnabled = false;
                    recordButton.IsEnabled = true;
                    openRecordingButton.IsEnabled = true;
                    Task.Factory.StartNew(() =>
                    {
                        int numFrames = 0;
                        lock (LockFubiUpdate)
                        {
                            Dispatcher.Invoke(new NoArgDelegate(enableLoadingCircle), null);
                            numFrames = Fubi.loadRecordedSkeletonData(Settings.Default.LastRecordingFilePath);
                            Dispatcher.Invoke(new NoArgDelegate(disableLoadingCircle), null);
                        }
                        if (numFrames > 0)
                        {
                            Dispatcher.Invoke((Action)(() =>
                            {
                                m_xmlGenerator.RecOptions.PlaybackFile = GetRelativePath(Directory.GetCurrentDirectory() + "\\",
                                    Settings.Default.LastRecordingFilePath);
                                playbackSlider.EndValue = playbackSlider.Maximum = numFrames - 1;
                                playbackSlider.Value = playbackSlider.StartValue = 0;
                                playButton.IsEnabled = trimButton.IsEnabled = true;
                                // Check if template recognizer options already were valid, but needed a recording file to be loaded
                                // (needs to be done after setting slider values!)
                                if (getSelectedXmlGenRecType() == XMLGenerator.RecognizerType.TemplateRecording)
                                    m_xmlGenOptionsWnd.checkSelections();
                            }));
                        }
                    });
                }
                else if (Fubi.isPlayingSkeletonData())
                {
                    Fubi.stopPlayingRecordedSkeletonData();
                    playbackSlider.Value = playbackSlider.StartValue;
                    pauseButton.IsEnabled = stopButton.IsEnabled = false;
                    playButton.IsEnabled = trimButton.IsEnabled = recordButton.IsEnabled = true;
                }
            }
        }

        private void waitForTarget()
        {
        }

        private void recordButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                FileName = Path.GetFileName(Settings.Default.LastRecordingFilePath),
                InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Settings.Default.LastRecordingFilePath)),
                DefaultExt = ".xml",
                AddExtension = true,
                Filter = "XML documents (.xml)|*.xml"
            };
            // Show open file dialog box 
            var result = dlg.ShowDialog();
            // Process open file dialog box results 
            if (result == true && dlg.FileName != null)
            {
                //#ifdef ADD_RP_2015
                if (recordImageCheckBox.IsChecked == true)
                {
                    aviFile.fileName = dlg.FileName.Substring(0, dlg.FileName.Length - 3) + "avi";
                    //var dlg2 = new SaveFileDialog
                    //{
                    //    FileName = Path.GetFileName(aviFileName),   //Settings.Default.LastRecordingFilePath
                    //    InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Settings.Default.LastRecordingFilePath)),
                    //    DefaultExt = ".avi",
                    //    AddExtension = true,
                    //    Filter = "AVI documents (.avi)|*.avi"
                    //};
                    //// Show open file dialog box 
                    //result = dlg2.ShowDialog();
                    //// Process open file dialog box results 
                    //if (result == true && dlg2.FileName != null)
                    //{
                    //    aviFileName = dlg2.FileName;
                    //}
                    aviFile.startSave();
                }
                //#endif

                recordSkeletonFileName = dlg.FileName;
                backgroundThread = new Thread(recordStartThread);
                isHand = useHandCheckBox.IsChecked == true;
                backgroundThread.Start();
            }
        }

        private void recordStartThread()
        {

            // Open document 
            DateTime startRecordTime = DateTime.Now;
            Double dt = 0;
            do
            {
                //int targets = 0;
                //lock (LockFubiUpdate)
                //{
                //    targets = isHand ? Fubi.getNumHands() : Fubi.getNumUsers();
                //}
                if (dt > 15.0 || lastNumUsers > 0)
                    break;
                Thread.Sleep(2000);
                Console.WriteLine("Waiting for target:" + lastNumUsers.ToString());
                dt = (DateTime.Now - startRecordTime).TotalSeconds;
            } while (true);  // wait 5 seconds if no human or hand
            lock (LockFubiUpdate)
            {
                uint targetID;
                targetID = isHand ? Fubi.getClosestHandID() : Fubi.getClosestUserID();
                Console.WriteLine("TargetID:" + targetID.ToString());
                recordingStarted = Fubi.startRecordingSkeletonData(recordSkeletonFileName, targetID, false, isHand);
            }
        }


        private void openRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                FileName = Path.GetFileName(Settings.Default.LastRecordingFilePath),
                InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Settings.Default.LastRecordingFilePath)),
                DefaultExt = ".xml",
                Filter = "XML documents (.xml)|*.xml"
            };
            // Show open file dialog box 
            var result = dlg.ShowDialog();


            // Process open file dialog box results 
            if (result == true && dlg.FileName != null)
            {
                //#ifdef ADD_RP_2015
                //if (recordImageCheckBox.IsChecked == true)  // need to save it even if we dont use it
                //{
                aviFile.fileName = dlg.FileName.Substring(0, dlg.FileName.Length - 3) + "avi";
                if (!File.Exists(aviFile.fileName) && (recordImageCheckBox.IsChecked == true))
                {
                    showWarnMsg("Video file " + aviFile.fileName + " does not exist.", "OPERATION ABORTED");
                    return;
                }
                //var dlg2 = new OpenFileDialog
                //{
                //    FileName = Path.GetFileName(Settings.Default.LastRecordingFilePath),
                //    InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Settings.Default.LastRecordingFilePath)),
                //    DefaultExt = ".avi",
                //    Filter = "AVI documents (.avi)|*.avi"
                //};
                //// Show open file dialog box 
                //var result2 = dlg2.ShowDialog();
                //if (result2 == true && dlg2.FileName != null)
                //{
                //    aviFileName = dlg2.FileName;
                //}
                //}
                //endif



                Task.Factory.StartNew(() =>
                {
                    int numFrames;
                    lock (LockFubiUpdate)
                    {
                        Dispatcher.Invoke(new NoArgDelegate(enableLoadingCircle), null);
                        numFrames = Fubi.loadRecordedSkeletonData(dlg.FileName);
                        Dispatcher.Invoke(new NoArgDelegate(disableLoadingCircle), null);
                    }
                    if (numFrames > 0)
                    {
                        Dispatcher.Invoke((Action)(() =>
                        {
                            m_xmlGenerator.RecOptions.PlaybackFile = GetRelativePath(Directory.GetCurrentDirectory() + "\\", dlg.FileName);
                            int current = 0, start = 0, end = numFrames - 1;
                            Fubi.getPlaybackMarkers(ref current, ref start, ref end);
                            playbackSlider.StartValue = start;
                            playbackSlider.Value = current;
                            playbackSlider.Maximum = numFrames - 1;
                            playbackSlider.EndValue = end;
                            playbackSlider.TickFrequency = (numFrames > 50.0) ? (numFrames / 50.0) : 1.0;
                            pauseButton.IsEnabled = stopButton.IsEnabled = false;
                            playButton.IsEnabled = trimButton.IsEnabled = true;
                            Settings.Default.LastRecordingFilePath = dlg.FileName;
                            Settings.Default.Save();
                            // Check if template recognizer options already were valid, but needed a recording file to be loaded
                            // (needs to be done after setting slider values!)
                            if (getSelectedXmlGenRecType() == XMLGenerator.RecognizerType.TemplateRecording)
                                m_xmlGenOptionsWnd.checkSelections();
                        }));
                    }
                });
            }
        }
        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            lock (LockFubiUpdate)
            {
                Fubi.pausePlayingRecordedSkeletonData();
            }
            pauseButton.IsEnabled = false;
            playButton.IsEnabled = trimButton.IsEnabled = stopButton.IsEnabled = true;
            //#ifdef ADD_RP_2015
            aviFile.pause = true;
            //#endif
        }

        private void playbackSliderMouseDown(object sender, EventArgs e)
        {
            lock (LockFubiUpdate)
            {
                // Temporarily pause the playback until the thumb is released again
                Fubi.pausePlayingRecordedSkeletonData();
            }
        }

        private void playbackSliderMouseUp(object sender, EventArgs e)
        {
            lock (LockFubiUpdate)
            {
                // Update the markers and restart playing (but only if was playing before)
                Fubi.setPlaybackMarkers(playbackSlider.Value, playbackSlider.StartValue, playbackSlider.EndValue);
                if (Fubi.isPlayingSkeletonData() && pauseButton.IsEnabled)
                    Fubi.startPlayingSkeletonData(repeatButton.IsChecked == true);
            }
        }
        private void playbackSliderStartValueChanged(object sender, EventArgs e)
        {
            lock (LockFubiUpdate)
            {
                Fubi.setPlaybackMarkers(playbackSlider.Value, playbackSlider.StartValue, playbackSlider.EndValue);
            }
        }
        private void playbackSliderEndValueChanged(object sender, EventArgs e)
        {
            lock (LockFubiUpdate)
            {
                Fubi.setPlaybackMarkers(playbackSlider.Value, playbackSlider.StartValue, playbackSlider.EndValue);
            }
        }

        private void repeatButtonCheckChange(object sender, RoutedEventArgs e)
        {
            lock (LockFubiUpdate)
            {
                // Update loop state only if currently playing
                if (Fubi.isPlayingSkeletonData())
                    Fubi.startPlayingSkeletonData(repeatButton.IsChecked == true);
            }
        }

        private void playbackSliderThumbDragDelta(object sender, EventArgs e)
        {
            lock (LockFubiUpdate)
            {
                Fubi.setPlaybackMarkers(playbackSlider.Value, playbackSlider.StartValue, playbackSlider.EndValue);
            }
        }

        private void xmlGenUseRecordingChanged(object sender, RoutedEventArgs e)
        {
            xmlGenStartingCountdownControl.IsEnabled = xmlGenRecDurationControl.IsEnabled = xmlGenUseRecording.IsChecked == false;
        }

        private void trimButton_Click(object sender, RoutedEventArgs e)
        {
            lock (LockFubiUpdate)
            {
                if (Fubi.trimPlaybackFileToMarkers())
                {
                    //#ifdef ADD_RP_2015
                    if (recordImageCheckBox.IsChecked == true)
                    {
                    }
                    //#endif
                    int current = 0, start = 0, end = 0;
                    Fubi.getPlaybackMarkers(ref current, ref start, ref end);
                    playbackSlider.Maximum = playbackSlider.EndValue = end;
                    playbackSlider.Value = current;
                    playbackSlider.StartValue = start;
                    playbackSlider.TickFrequency = ((end + 1) > 50.0) ? ((end + 1) / 50.0) : 1.0;
                }
                else
                    showWarnMsg("Unable to trim file!", "Trim Error");
            }
        }

        private void xmlGenUseHandChanged(object sender, RoutedEventArgs e)
        {
            if (m_xmlGenOptionsWnd != null)
                m_xmlGenOptionsWnd.UseHand = xmlGenUseHand.IsChecked == true;
        }

        private void recognitionStart(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
        {
            Dispatcher.BeginInvoke((Action)(
                () =>
                {
                    switch (recognizerType)
                    {
                        case FubiUtils.RecognizerType.PREDEFINED_GESTURE:
                            {
                                if (predefinedCheckBox.IsChecked == true && checkBox1.IsChecked == true)
                                {
                                    textBox1.Text += (isHand ? "Hand " : "User ") + targetID + ": START OF " + gestureName + " -->\n";
                                    textBox1.ScrollToEnd();
                                }
                                if (m_enableKeyMouseBinding)
                                    m_fubiMouseKeyboard.applyBinding(gestureName, true);
                            }
                            break;
                        case FubiUtils.RecognizerType.USERDEFINED_GESTURE:
                            {
                                if (checkBox1.IsChecked == true)
                                {
                                    textBox1.Text += (isHand ? "Hand " : "User ") + targetID + ": START OF " + gestureName + " -->\n";
                                    textBox1.ScrollToEnd();
                                }
                                if (m_enableKeyMouseBinding)
                                    m_fubiMouseKeyboard.applyBinding(gestureName, true);
                            }
                            break;
                        case FubiUtils.RecognizerType.PREDEFINED_COMBINATION:
                            {
                                if (predefinedCheckBox.IsChecked == true && checkBox2.IsChecked == true)
                                    textBox2.Text += "User" + targetID + ": " + gestureName + "\n";
                                if (m_enableKeyMouseBinding)
                                {
                                    m_fubiMouseKeyboard.applyBinding(gestureName, true);
                                    m_fubiMouseKeyboard.applyBinding(gestureName, false);
                                }
                            }
                            break;
                        case FubiUtils.RecognizerType.USERDEFINED_COMBINATION:
                            {
                                if (checkBox2.IsChecked == true)
                                    textBox2.Text += "User" + targetID + ": " + gestureName + "\n";
                                if (m_statsWindow != null)
                                {
                                    if (m_statsWindow.Recognitions.ContainsKey(targetID) == false)
                                        m_statsWindow.Recognitions[targetID] = new Dictionary<string, double>();
                                    m_statsWindow.Recognitions[targetID][gestureName] = Fubi.getCurrentTime();
                                }
                                if (m_enableKeyMouseBinding)
                                {
                                    m_fubiMouseKeyboard.applyBinding(gestureName, true);
                                    m_fubiMouseKeyboard.applyBinding(gestureName, false);
                                }
                            }
                            break;
                    }
                }));
        }

        private void recognitionEnd(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
        {
            Dispatcher.BeginInvoke((Action)(
                () =>
                {
                    switch (recognizerType)
                    {
                        case FubiUtils.RecognizerType.PREDEFINED_GESTURE:
                            {
                                if (predefinedCheckBox.IsChecked == true && checkBox1.IsChecked == true)
                                {
                                    textBox1.Text += "-->" + (isHand ? "Hand " : "User ") + targetID + ": END OF " + gestureName + "\n";
                                    textBox1.ScrollToEnd();
                                }
                                if (m_enableKeyMouseBinding)
                                    m_fubiMouseKeyboard.applyBinding(gestureName, false);
                            }
                            break;
                        case FubiUtils.RecognizerType.USERDEFINED_GESTURE:
                            {
                                if (checkBox1.IsChecked == true)
                                {
                                    textBox1.Text += "-->" + (isHand ? "Hand " : "User ") + targetID + ": END OF " + gestureName + "\n";
                                    textBox1.ScrollToEnd();
                                }
                                if (m_enableKeyMouseBinding)
                                    m_fubiMouseKeyboard.applyBinding(gestureName, false);
                            }
                            break;
                        case FubiUtils.RecognizerType.USERDEFINED_COMBINATION:
                        case FubiUtils.RecognizerType.PREDEFINED_COMBINATION:
                            {
                                // Nothing to do here..
                            }
                            break;
                    }
                }));
        }
    }
}