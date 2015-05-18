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

using System.IO;


using FubiNET;
//using Microsoft.Win32;
//using System.Windows.Data;

using Video;
using System.Windows.Forms;  //add references to System.Windows.Forms from .Net group and System.Drawing for screen control


namespace SpiderDanceOff
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
        private VideoClass videoFile = new VideoClass();
        int frameNr = 0;
        bool stopDepthVideo = false;
        bool stopFubi = false;

        // Threading
        public readonly object LockFubiUpdate = new object(); // identifies sections that only one thread may enter
        // Delegate for triggering gui message updates from separate threads
        private delegate void NoArgDelegate();
        private delegate void OneStringDelegate(string str);
        private delegate void TwoStringDelegate(string str1, string str2);

        private string mediaPath = @"E:\Documents\media\spiders";
        private string[] gameStageGesture = { "LeftHandWavingAboveShoulder OR RightHandWavingAboveShoulder", "hipWobble", "ArmsUpDownUp", "HandsJoinUp", "sideStep", "armsUp AND hipWobble", "sideStep" };
        private string spiderDancePath = "spiderDances";
        private string[] gameStageVideos = { "spider_wave.vgz", "spider_hip_wobble.vgz", "spider_arms_up_down.vgz", "spider_hands_join_up.vgz", "spider_side_step.vgz", "spider_hip_wobble_arms_up.vgz", "spider_side_step.vgz", "success.vgz", "failure.vgz" };
        //private bool isHipWobble = false;

        Window1 window1 = null;
        int gameStage = 0;
        List<string> activeGestures = new List<string>();
        bool gestureStep = false;
        DateTime stageTime;
        double stageTimeLimit = 10.0;
        int testMode = 0;  // 1 = kills timeout
        string statusMessage = "";
        bool mvPlaying = false;
        bool fvPlaying = false;
        bool secondWindow = false;
        bool videoPlaying = false;
        bool filterActive = true;


        public MainWindow()
        {
            InitializeComponent();
            m_running = true;
            m_fubiThread = new Thread(fubiMain);

            // testtest();
        }

        public void showWarnMsg(string message, string caption)
        {
            System.Windows.MessageBox.Show(this, message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (secondWindow)
            {
                Screen[] screens = Screen.AllScreens; // 'doesn't necessarily put them in the right order
                foreach (Screen s in screens)
                {
                    if (SystemInformation.MonitorCount == 1 || s.WorkingArea.Left > 1000)
                    {
                        Screen screen2 = s;
                        Console.WriteLine("Screen width:" + s.WorkingArea.Width.ToString() + " height:" + s.WorkingArea.Height.ToString());
                        window1 = new Window1();
                        window1.WindowStartupLocation = WindowStartupLocation.Manual;
                        window1.Left = screen2.WorkingArea.Left;
                        window1.Top = screen2.WorkingArea.Top;
                        window1.Show();
                        window1.WindowState = WindowState.Maximized; //do this after Show() or won't draw on secondary screens
                    }
                }
            }
            if (SystemInformation.MonitorCount == 1)
            {
                this.WindowState = System.Windows.WindowState.Minimized;
            }
            else
            {
                maxWindow();
            }
            string fid = System.IO.Path.Combine(mediaPath, @"Peacock_spider__(Maratus_volans)__MaleWave.mp4-.mp4");
            maleSpiderVideo.Source = new System.Uri(fid);
            if (secondWindow)
            {
                fid = System.IO.Path.Combine(mediaPath, @"Peacock_spider__(Maratus_volans)_female1.mp4");
                window1.femaleSpiderVideo.Source = new System.Uri(fid);
            }
            
            videoFile.edgeDelta = 10;
            videoFile.edgeThickness = 3;
            // Only start the thread after the windows has loaded
            m_fubiThread.Start(new FubiUtils.FilterOptions(1.0f, 1.0f, 0.007f));  // using filter defaults

            // new to wait until fubi thread has started before playing video (found using keyboard stop start controls)
            //maleSpiderVideo.Play();
            //window1.femaleSpiderVideo.Play();
            mvPlaying = false;
            fvPlaying = false;
            setProgress(0);

        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            maleSpiderVideo.Stop();
            //window1.femaleSpiderVideo.Stop();
            m_running = false;

            m_fubiThread.Join(2000);

            Dispatcher.InvokeShutdown();
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
                case Key.Escape:
                    Close();
                    break;
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
                case Key.M:
                    mvPlaying = !mvPlaying;
                    if (mvPlaying)
                        maleSpiderVideo.Play();
                    else
                        maleSpiderVideo.Stop();
                    break;
                case Key.V:
                    if (secondWindow)
                    {
                        fvPlaying = !fvPlaying;
                        if (fvPlaying)
                            window1.femaleSpiderVideo.Play();
                        else
                            window1.femaleSpiderVideo.Stop();
                    }
                    break;
                case Key.F:
                    stopFubi = !stopFubi;
                    Console.WriteLine("Fubi state:" + stopDepthVideo);
                    break;
                case Key.S:
                    stopDepthVideo = !stopDepthVideo;
                    Console.WriteLine("Depth video state:" + stopDepthVideo);
                    break;
                case Key.L:
                    filterActive = !filterActive;
                    Console.WriteLine("Filter state:" + filterActive);
                    break;

                case Key.Space:
                    setProgress(gameStage + 1);
                    break;
                case Key.Tab:
                    testMode = 1 - testMode;
                    break;
            //    case Key.P:
            //        Console.WriteLine("Pos1:" + window1.femaleSpiderVideo.Position);
            //        if (window1.femaleSpiderVideo.IsLoaded)
            //        {
            //            //window1.femaleSpiderVideo.Stop();
            //            //window1.femaleSpiderVideo.Play();
            //        }
            //        Console.WriteLine("Pos2:" + window1.femaleSpiderVideo.Position);
            //        Console.WriteLine("Duration:" + window1.femaleSpiderVideo.NaturalDuration);
            //        int seconds = (int)(window1.femaleSpiderVideo.NaturalDuration.TimeSpan.TotalSeconds / 10.0);
            //        window1.femaleSpiderVideo.Position += new TimeSpan(0, 0, seconds);
            //        Console.WriteLine("Pos3:" + window1.femaleSpiderVideo.Position);
            //        break;
            }
        }

        private void setProgress(int stage)
        {
            activeGestures.Clear();
            gameStage = stage;
            stageTime = DateTime.Now;
            System.Windows.Controls.Label[] stageLabels = { progressLbl1, progressLbl2, progressLbl3, progressLbl4, progressLbl5, progressLbl6, progressLbl7, progressLbl8 };
            for (int i = 0; i < stageLabels.Length; i++)
            {
                if (i == stage)
                {
                    stageLabels[i].Foreground = Brushes.Yellow;
                    stageLabels[i].Background = Brushes.Green;
                }
                else
                {
                    stageLabels[i].Foreground = Brushes.Brown;
                    stageLabels[i].Background = Brushes.Gray;
                }
                stageLabels[i].FontSize = 32;
            }
            if (stage == 7)
            {
                statusLbl.Content = "SUCCESS! SHE LOVES MY DANCE.";
                statusLbl.Visibility = Visibility.Visible;
            }
            else if (stage == 8)
            {
                statusLbl.Content = "FAILURE. RUN AWAY!";
                statusLbl.Visibility = Visibility.Visible;
            }
            else
                statusLbl.Visibility = Visibility.Hidden;

            videoFile.stopPlay();
            if (gameStage <= gameStageVideos.Length)
            {
                videoFile.fileName = System.IO.Path.Combine(spiderDancePath, gameStageVideos[gameStage]);
                videoFile.startPlayback();
            }
            TimeOutBar.Width = 0;
        }

        private void fubiMain(object filterOptions)
        {

            m_renderOptions = 0;
            m_renderOptions |= (int)FubiUtils.RenderOptions.Shapes;
            //m_renderOptions |= (int)FubiUtils.RenderOptions.Skeletons;
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
                if (Fubi.loadRecognizersFromXML("SpiderRecognizers.xml"))
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
            //videoFile.fileName = gameStageVideos[gameStage]; //"trainingData/tempRecord2.vid";
            //videoFile.startPlayback();

            while (m_running)
            {

                lock (LockFubiUpdate)
                {
                    if (!stopFubi)
                    {

                        // Now update the sensors
                        Fubi.updateSensor();

                        Fubi.getImage(s_buffer2, FubiUtils.ImageType.Depth, m_numChannels, FubiUtils.ImageDepth.D8, m_renderOptions,
                                (int)FubiUtils.JointsToRender.ALL_JOINTS, m_selectedDepthMod);
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
                if (videoFile.playMode && !stopDepthVideo)  // && Fubi.isPlayingSkeletonData())
                {
                    frameNr = videoFile.readFrame(s_buffer1, frameNr);
                    var stride1 = windowBuffer1.PixelWidth * (windowBuffer1.Format.BitsPerPixel / 8);
                    windowBuffer1.WritePixels(new Int32Rect(0, 0, windowBuffer1.PixelWidth, windowBuffer1.PixelHeight), s_buffer1, stride1, 0);
                }
                //}
                //catch (Exception ex)
                //{
                //    System.Windows.MessageBox.Show(ex.ToString());
                //}

                var stride2 = windowBuffer2.PixelWidth * (windowBuffer2.Format.BitsPerPixel / 8);
                //int c = BitConverter.ToInt32(s_buffer2, (stride2 * windowBuffer2.PixelHeight+ stride2)/2);  //sample center
                //Console.WriteLine(c.ToString("x"));  // seems to be ARGB format as printed (ie byte reverse)
                if(filterActive)
                  s_buffer2 = videoFile.edgeFilter(s_buffer2);
                windowBuffer2.WritePixels(new Int32Rect(0, 0, windowBuffer2.PixelWidth, windowBuffer2.PixelHeight), s_buffer2, stride2, 0);
            }

            if (gestureStep)
            {
                gestureStep = false;
                setProgress(gameStage + 1);
            }
            else if (gameStage > 0)
            {
                double stage_time = (DateTime.Now - stageTime).TotalSeconds;
                if (stage_time > stageTimeLimit)
                {
                    if (testMode == 0)
                    {
                        if (gameStage > 6)
                            setProgress(0);  // fail notice should appear if gamestage > 0 and user still present
                        else
                            setProgress(8);  // note that stage 7 is the success stage
                    }
                    else
                    {
                        TimeOutBar.Width = 0;
                    }
                }
                else if (gameStage > 0)
                {
                    TimeOutBar.Width = stage_time / stageTimeLimit * progressStackPanel.ActualWidth;
                }
                else
                    TimeOutBar.Width = 0;
            }
            if (statusMessage.Length > 0)  // used in test modes
            {
                statusLbl.Content = statusMessage;
                statusLbl.Visibility = Visibility.Visible;
                statusMessage = "";
            }

            if(!videoPlaying) {
                videoPlaying = true;
                mvPlaying = true;
                maleSpiderVideo.Play();
                if(secondWindow) {
                    fvPlaying = true;
                    window1.femaleSpiderVideo.Play();
                }
            }



                
        }

        private void recognitionStart(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
        {
            switch (recognizerType)
            {
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
                case FubiUtils.RecognizerType.USERDEFINED_COMBINATION:
                    //        case FubiUtils.RecognizerType.PREDEFINED_COMBINATION:
                    //            {
                    //                // Nothing to do here..
                    //            }
                    Console.WriteLine(recognizerType.ToString() + "-->" + "User " + targetID + ": START OF " + gestureName + "\n");
                    uint numStates;
                    bool isInterrupted, isInTransition;
                    int gestureState = Fubi.getCurrentCombinationRecognitionState(gestureName, targetID, out numStates, out isInterrupted, out isInTransition) + 1;
                    Console.WriteLine("State:" + gestureState + " NumStates:" + numStates + " IsInterupted:" + isInterrupted + " IsInTransition:" + isInTransition);
                    activeGestures.Add(gestureName);
                    //if (true) // (gestureState == numStates)
                    //{
                    //    if (gestureName == gameStageGesture[gameStage])
                    //    {
                    //        if (gameStage != 5 || isHipWobble)
                    //            setProgress(gameStage + 1);
                    //    }
                    //    else if (gestureName == "hipWobble")
                    //        isHipWobble = true;
                    //}
                    if (gameStage < gameStageGesture.Length)
                        gestureStep = testExpression(gameStageGesture[gameStage], activeGestures);    // lets gui update know to advance to next stage
                    break;
            }
        }

        //private void testtest()
        //{
        //    string exp1 = "A AND B";
        //    string exp2 = "A OR B";
        //    List<string> states = new List<string>();
        //    Console.WriteLine("States=" + states.ToString());
        //    Console.WriteLine(exp1 + " = " + testExpression(exp1, states).ToString());
        //    Console.WriteLine(exp2 + " = " + testExpression(exp2, states).ToString());
        //    states.Add("A");
        //    Console.WriteLine("States=" + states.ToString());
        //    Console.WriteLine(exp1 + " = " + testExpression(exp1, states).ToString());
        //    Console.WriteLine(exp2 + " = " + testExpression(exp2, states).ToString());
        //    states.Add("B");
        //    Console.WriteLine("States=" + states.ToString());
        //    Console.WriteLine(exp1 + " = " + testExpression(exp1, states).ToString());
        //    Console.WriteLine(exp2 + " = " + testExpression(exp2, states).ToString());
        //    states.Remove("A");
        //    Console.WriteLine("States=" + states.ToString());
        //    Console.WriteLine(exp1 + " = " + testExpression(exp1, states).ToString());
        //    Console.WriteLine(exp2 + " = " + testExpression(exp2, states).ToString());
        //}

        private bool testExpression(string expression, List<string> active_states)
        {
            // expression is a boolean math expression formed by AND, OR and () operators
            // this is tested against the presence of arguments in a list of active_states
            char[] seperator = { ' ' };
            string[] gest_script = expression.Split(seperator);
            bool[] state = new bool[10];  // assumption
            int[] op = new int[10];
            state[0] = false;
            op[0] = 0;
            int bl = 0;
            foreach (string gs in gest_script)
            {
                if (gs == "(")
                {
                    bl++;
                    op[bl] = 0;
                }
                else if (gs == ")")
                {
                    if (bl > 0)
                    {
                        switch (op[bl - 1])
                        {
                            case 0:
                                state[bl - 1] = state[bl];
                                break;
                            case 1:
                                state[bl - 1] = state[bl - 1] || state[bl];
                                break;
                            case 2:
                                state[bl - 1] = state[bl - 1] && state[bl];
                                break;
                        }
                        bl--;
                    }
                }
                else if (gs == "OR")
                    op[bl] = 1;
                else if (gs == "AND")
                    op[bl] = 2;
                else
                {
                    bool gstate = active_states.Contains(gs);
                    if (gstate && (testMode == 1))
                    {
                        statusMessage = gs;
                    }
                    switch (op[bl])
                    {
                        case 0:
                            state[bl] = gstate;
                            break;
                        case 1:
                            state[bl] = gstate || state[bl];
                            break;
                        case 2:
                            state[bl] = gstate && state[bl];
                            break;
                    }

                }
            }
            return state[0];  // if true all conditions have been achieived
        }

        private void recognitionEnd(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
        {
            switch (recognizerType)
            {
                case FubiUtils.RecognizerType.USERDEFINED_COMBINATION:
                    Console.WriteLine(recognizerType.ToString() + "-->" + "User " + targetID + ": END OF " + gestureName + "\n");
                    //activeGestures.Remove(gestureName);  - gestures are active until change of stage
                    //if (gestureName == "hipWobble")
                    //    isHipWobble = false;
                    break;
            }
        }

        private void MediaElement_MediaOpened(System.Object sender, EventArgs e)
        {
            Console.WriteLine("Opened:" + sender.ToString());
        }


        private void MediaElement_MediaFailed(System.Object sender, EventArgs e)
        {
            Console.WriteLine("Media failed: " + e.ToString());
            //DiagnosticLabel.Content = "Media failed: " & e.ToString
            //'MsgBox("Media failed: " & e.ToString)
            maleSpiderVideo.Stop();
        }

        private void MediaElement_MediaEnded(System.Object sender, EventArgs e)
        {
            maleSpiderVideo.Stop();
            maleSpiderVideo.Play();
        }

    }
}
