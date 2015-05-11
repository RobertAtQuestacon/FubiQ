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
        public Window1()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            femaleSpiderVideo.Stop();
        }

                private void MediaElement_MediaOpened(System.Object sender, EventArgs e)
        {
        }


        private void MediaElement_MediaFailed(System.Object sender, EventArgs e)
        {
            Console.WriteLine("Media failed: " + e.ToString());
            //DiagnosticLabel.Content = "Media failed: " & e.ToString
            //'MsgBox("Media failed: " & e.ToString)
            femaleSpiderVideo.Stop();
        }

        private void MediaElement_MediaEnded(System.Object sender, EventArgs e)
        {
            femaleSpiderVideo.Stop();
            femaleSpiderVideo.Play();
        }

    }
}
