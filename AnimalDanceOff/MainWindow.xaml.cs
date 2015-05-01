using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;  // for Thread objects
using System.Windows.Threading;  // for DispatchOperation
using System.ComponentModel;   // for  cancelEventArgs


using FubiNET;
//using Microsoft.Win32;
//using System.Windows.Data;

using Video;

namespace AnimalDanceOff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // The current Fubi settings (first modified by the GUI and then taken by Fubi)
        FubiUtils.SensorType m_selectedSensor = FubiUtils.SensorType.OPENNI1;  // for ASUS xtion
        FubiUtils.DepthImageModification m_selectedDepthMod = FubiUtils.DepthImageModification.UseHistogram;
        int m_renderOptions = (int)FubiUtils.RenderOptions.Default;

        // The thread for updating Fubi separately to the GUI
        private readonly Thread m_fubiThread;
        private bool m_running;

        FubiUtils.ImageNumChannels m_numChannels = FubiUtils.ImageNumChannels.C4; // influences the initial buffer size!
        // for image type FubiUtils.ImageType.Color  m_numChannels = FubiUtils.ImageNumChannels.C3;
        // for image type  FubiUtils.ImageType.IR m_numChannels = FubiUtils.ImageNumChannels.C3;
        // for image type FubiUtils.ImageType.Blank  m_numChannels = FubiUtils.ImageNumChannels.C4;

        Int32 m_width = 640, m_height = 480;	// influences the initial buffer size! - reset by getDepthResolution
        WriteableBitmap windowBuffer1;
        WriteableBitmap windowBuffer2;
        private static byte[] s_buffer1 = null;  //set to null to indicate it needs to be set
        private static byte[] s_buffer2;
        private VideoClass aviFile = new VideoClass();
        int frameNr = 0;

        // Threading
        public readonly object LockFubiUpdate = new object(); // identifies sections that only one thread may enter
        // Delegate for triggering gui message updates from separate threads
        private delegate void NoArgDelegate();
        private delegate void OneStringDelegate(string str);
        private delegate void TwoStringDelegate(string str1, string str2);

        public MainWindow()
        {
            InitializeComponent();
            m_running = true;
            m_fubiThread = new Thread(fubiMain);

        }

        public void showWarnMsg(string message, string caption)
        {
            MessageBox.Show(this, message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Only start the thread after the windows has loaded
            m_fubiThread.Start(new FubiUtils.FilterOptions(1.0f, 1.0f, 0.007f));  // using filter defaults
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            m_running = false;

            m_fubiThread.Join(2000);

            Dispatcher.InvokeShutdown();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }

        private void maxWindow()
        {
            //this.Height = 480;
            // this.Width = 640;
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
        }

        private void onKeyPress(Object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    if (this.WindowState == WindowState.Minimized)
                    {  //this happens in the one screen situation
                        this.WindowState = WindowState.Maximized;
                        return;
                    }
                    if (this.Height == 1000)
                    {
                        maxWindow();
                    }
                    else
                    {
                        this.Height = 1000;
                        this.Width = 1000;
                        this.WindowState = WindowState.Normal;
                        this.WindowStyle = WindowStyle.SingleBorderWindow;
                    }
                    break;
            }
        }


        private void fubiMain(object filterOptions)
        {
            m_renderOptions = 0;
            m_renderOptions |= (int)FubiUtils.RenderOptions.Shapes;
            m_renderOptions |= (int)FubiUtils.RenderOptions.Skeletons;
            //m_renderOptions |= (int)FubiUtils.RenderOptions.UserCaptions;
            //m_renderOptions |= (int)FubiUtils.RenderOptions.Background;
            //m_renderOptions |= (int)FubiUtils.RenderOptions.SwapRAndB;
            // m_renderOptions |= (int)FubiUtils.RenderOptions.DetailedFaceShapes;
            //m_renderOptions |= (int)FubiUtils.RenderOptions.BodyMeasurements;
            // m_renderOptions |= (int)FubiUtils.RenderOptions.GlobalOrientCaptions; // or
            //m_renderOptions |= (int)FubiUtils.RenderOptions.LocalOrientCaptions;
            // m_renderOptions |= (int)FubiUtils.RenderOptions.GlobalPosCaptions;  // or
            //m_renderOptions |= (int)FubiUtils.RenderOptions.LocalPosCaptions;
            m_renderOptions |= (int)FubiUtils.RenderOptions.UseFilteredValues;

            var rgbOptions = new FubiUtils.StreamOptions();
            var irOptions = new FubiUtils.StreamOptions();
            var depthOptions = new FubiUtils.StreamOptions();
            var invalidOptions = new FubiUtils.StreamOptions(-1);
            lock (LockFubiUpdate)
            {

                if (!Fubi.init(new FubiUtils.SensorOptions(depthOptions, rgbOptions, irOptions, m_selectedSensor),
                        (FubiUtils.FilterOptions)filterOptions))
                {   // the following for when the init fails
                    m_selectedSensor = FubiUtils.SensorType.NONE;
                    Fubi.init(new FubiUtils.SensorOptions(depthOptions, invalidOptions, invalidOptions, m_selectedSensor),
                        (FubiUtils.FilterOptions)filterOptions);
                    Dispatcher.BeginInvoke(new TwoStringDelegate(showWarnMsg),
                        new object[]
						{
							"Error starting sensor! \nDid you connect the sensor and install the correct driver? \nTry selecting a different sensor.",
							"Error starting sensor"
						});
                    return;
                }
                Fubi.getDepthResolution(out m_width, out m_height);
                m_numChannels = FubiUtils.ImageNumChannels.C4;

                // All known combination recognizers will be started automatically for new users
                Fubi.setAutoStartCombinationRecognition(true);

                // Load XML with sample mouse control gestures
                if (Fubi.loadRecognizersFromXML("TutorialRecognizers.xml"))
                {
                    //    // This requires to update the gesture list used for selecting key/button bindings and for xml generation
                    //    Dispatcher.BeginInvoke(new NoArgDelegate(refreshGestureList), null);
                    //    // Now we can load the default bindings using the above recognizers
                    //    Dispatcher.BeginInvoke(new OneStringDelegate(m_fubiMouseKeyboard.Bindings.loadFromXML), "KeyMouseBindings.xml");
                }
            }

            Fubi.RecognitionStart += new Fubi.RecognitionHandler(recognitionStart);
            Fubi.RecognitionEnd += new Fubi.RecognitionHandler(recognitionEnd);
            DispatcherOperation currentOp = null;
            aviFile.fileName = "trainingData/tempRecord2.vid";
            aviFile.startPlayback();

            while (m_running)
            {

                lock (LockFubiUpdate)
                {
                    // Now update the sensors
                    Fubi.updateSensor();

                    Fubi.getImage(s_buffer2, FubiUtils.ImageType.Depth, m_numChannels, FubiUtils.ImageDepth.D8, m_renderOptions,
                            (int)FubiUtils.JointsToRender.ALL_JOINTS, m_selectedDepthMod);
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

        private void updateGUI()
        {
            // Display image
            if (m_width > 0 && m_height > 0)
            {

                if (s_buffer1 == null)
                {
                    var format = PixelFormats.Bgra32;
                    windowBuffer1 = new WriteableBitmap(m_width, m_height, 0, 0, format, BitmapPalettes.Gray256);
                    s_buffer1 = new byte[(int)m_numChannels * m_width * m_height];
                    image1.Source = windowBuffer1;
                    windowBuffer2 = new WriteableBitmap(m_width, m_height, 0, 0, format, BitmapPalettes.Gray256);
                    s_buffer2 = new byte[(int)m_numChannels * m_width * m_height];
                    image2.Source = windowBuffer2;
                }
                //try
                //{
                if (aviFile.playMode)  // && Fubi.isPlayingSkeletonData())
                {
                    frameNr = aviFile.readFrame(s_buffer1, frameNr);
                    var stride1 = windowBuffer1.PixelWidth * (windowBuffer1.Format.BitsPerPixel / 8);
                    windowBuffer1.WritePixels(new Int32Rect(0, 0, windowBuffer1.PixelWidth, windowBuffer1.PixelHeight), s_buffer1, stride1, 0);
                }
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show(ex.ToString());
                //}

                var stride2 = windowBuffer2.PixelWidth * (windowBuffer2.Format.BitsPerPixel / 8);
                //int c = BitConverter.ToInt32(s_buffer2, (stride2 * windowBuffer2.PixelHeight+ stride2)/2);  //sample center
                //Console.WriteLine(c.ToString("x"));  // seems to be ARGB format as printed (ie byte reverse)
                windowBuffer2.WritePixels(new Int32Rect(0, 0, windowBuffer2.PixelWidth, windowBuffer2.PixelHeight), s_buffer2, stride2, 0);
            }

        }

        private void recognitionStart(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
        {
            Console.WriteLine(recognizerType.ToString() + "-->" + "User " + targetID + ": START OF " + gestureName + "\n");
            //    switch (recognizerType)
            //    {
            //        case FubiUtils.RecognizerType.PREDEFINED_GESTURE:
            //            {
            //                Console.WriteLine("-->" + (isHand ? "Hand " : "User ") + targetID + ": END OF " + gestureName + "\n");
            //            }
            //            break;
            //        case FubiUtils.RecognizerType.USERDEFINED_GESTURE:
            //            {
            //                Console.WriteLine("-->" + (isHand ? "Hand " : "User ") + targetID + ": END OF " + gestureName + "\n");
            //                 }
            //            break;
            //        case FubiUtils.RecognizerType.USERDEFINED_COMBINATION:
            //        case FubiUtils.RecognizerType.PREDEFINED_COMBINATION:
            //            {
            //                // Nothing to do here..
            //            }
            //            break;
            //    }
        }

        private void recognitionEnd(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
        {

        }
    }
}
