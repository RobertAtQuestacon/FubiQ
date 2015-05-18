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
using System.Windows.Shapes;
using System.ComponentModel;   // for  cancelEventArgs

namespace SpiderDanceOff
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {

        //public MediaElement femaleSpiderVideo;
        //private string mediaPath = @"E:\Documents\media\spiders";
 
        public Window1()
        {
            InitializeComponent();
        }

        public void windowLoaded(object sender, RoutedEventArgs e) {

            // the follow was an attempt to have more reliable video play but it had not effect
            //femaleSpiderVideo = new MediaElement();
            //femaleSpiderVideo.LoadedBehavior = MediaState.Manual;
            //sideGrid.Children.Add(femaleSpiderVideo);
            //femaleSpiderVideo.Stretch = Stretch.UniformToFill;
            //femaleSpiderVideo.Loaded += MediaElement_Loaded;
            //femaleSpiderVideo.MediaOpened += MediaElement_MediaOpened;
            //femaleSpiderVideo.MediaFailed += MediaElement_MediaFailed;
            //femaleSpiderVideo.MediaEnded += MediaElement_MediaEnded;
            //string fid = System.IO.Path.Combine(mediaPath, @"Peacock_spider__(Maratus_volans)_female1.mp4");
            //femaleSpiderVideo.Source = new System.Uri(fid);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //femaleSpiderVideo.Stop();
        }

        private void MediaElement_Loaded(System.Object sender, EventArgs e)
        {
            Console.WriteLine("Loaded:" + sender.ToString());
            //femaleSpiderVideo.Play();
        }

        private void MediaElement_MediaOpened(System.Object sender, EventArgs e)
        {
            Console.WriteLine("OPened:" + sender.ToString());
        }


        private void MediaElement_MediaFailed(System.Object sender, EventArgs e)
        {
            Console.WriteLine("Media failed: " + e.ToString());
            //DiagnosticLabel.Content = "Media failed: " & e.ToString
            //'MsgBox("Media failed: " & e.ToString)
            //femaleSpiderVideo.Stop();
        }

        private void MediaElement_MediaEnded(System.Object sender, EventArgs e)
        {
            //femaleSpiderVideo.Stop();
            //femaleSpiderVideo.Play();
        }

    }
}
