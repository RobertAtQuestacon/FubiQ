using System;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.Linq;
using System.Reflection;
using System.Drawing;

using FubiNET;

namespace Fubi_WPF_GUI
{
    /// <summary>
    /// Interaktionslogik für RecognizerStatsWindow.xaml
    /// </summary>
    public partial class RecognizerStatsWindow
    {
        private readonly Thread m_updateThread;
        private bool m_running;
        private delegate void NoArgDelegate();

        public RecognizerStatsWindow()
        {
            InitializeComponent();

            this.FontSize = 24;

            m_updateThread = new Thread(update);
            m_running = true;
            m_updateThread.Start();
        }

        private void update()
        {
            while (m_running)
            {
                var currentOp = Dispatcher.BeginInvoke(new NoArgDelegate(updateStats), null);
                Thread.Sleep(150); // Don't update more often
                while (currentOp.Status != DispatcherOperationStatus.Completed
                    && currentOp.Status != DispatcherOperationStatus.Aborted)
                {
                    currentOp.Wait(TimeSpan.FromMilliseconds(50)); // If the update unexpectedly takes longer
                }
            }
        }

        public Dictionary<uint, Dictionary<string, double>> Recognitions = new Dictionary<uint, Dictionary<string, double>>();
        public Dictionary<uint, Dictionary<string, double>> HandRecognitions = new Dictionary<uint, Dictionary<string, double>>();

        public Dictionary<uint, Dictionary<string, string>> Hints = new Dictionary<uint, Dictionary<string, string>>();
        public Dictionary<uint, Dictionary<string, string>> HandHints = new Dictionary<uint, Dictionary<string, string>>();

        private void updateStats()
        {
            //Check all users
            var numUsers = Fubi.getNumUsers();
            var numHands = Fubi.getNumHands();

            if (numUsers == 0 && numHands == 0)
            {
                warnLabel.Visibility = Visibility.Visible;
            }
            else
                warnLabel.Visibility = Visibility.Hidden;

	        int currentIndex = 0;
            for (uint i = 0; i < numUsers+1; i++)
            {
                var id = (i == numUsers && Fubi.isPlayingSkeletonData()) ? FubiUtils.PlaybackUserID : Fubi.getUserID(i);
                if (id > 0)
                {
					// Not existent yet
					if (currentIndex >= statsTree.Items.Count)
					{
						statsTree.Items.Add(new TvUser());
						((TvUser)statsTree.Items[currentIndex]).IsExpanded = true;
					}
					// Wrong type, i.e. TvHand instead of TvUser
					if (statsTree.Items[currentIndex].GetType() != typeof(TvUser))
					{
						statsTree.Items[currentIndex] = new TvUser();
						((TvUser)statsTree.Items[currentIndex]).IsExpanded = true;
					}
	                var tUser = (TvUser) statsTree.Items[currentIndex];
					tUser.id = id;

                    // Update user defined combinations
                    var numRecs = Fubi.getNumUserDefinedCombinationRecognizers();
                    uint actualRecs = 0;
                    for (uint pc = 0; pc < numRecs; ++pc)
                    {
                        var name = Fubi.getUserDefinedCombinationRecognizerName(pc);
                        while (actualRecs >= tUser.Recs.Count)
                            tUser.Recs.Add(new TvRec());

                        var rec = tUser.Recs[(int)actualRecs];
                        rec.id = pc;
                        rec.name = name;
                        uint numStates;
                        bool isInterrupted, isInTransition;
                        rec.currState = Fubi.getCurrentCombinationRecognitionState(name, id, out numStates, out isInterrupted, out isInTransition) + 1;
                        rec.numStates = numStates;
                        rec.isInterrupted = isInterrupted;
                        rec.isInTransition = isInTransition;
                        if (Recognitions.ContainsKey(id) && Recognitions[id].ContainsKey(name) && Fubi.getCurrentTime() - Recognitions[id][name] < 2.0)
							rec.bgColor = "LightGreen";
                        else
                            rec.bgColor = Color.Transparent.Name;
                        if (Hints.ContainsKey(id) && Hints[id].ContainsKey(name))
                            rec.hint = Hints[id][name];
                        actualRecs++;
                    }

                    while (tUser.Recs.Count > actualRecs)
                    {
                        tUser.Recs.RemoveAt(tUser.Recs.Count - 1);
                    }

	                ++currentIndex;
                }
            }

            for (uint i = 0; i < numHands+1; i++)
            {
				var id = (i == numHands && Fubi.isPlayingSkeletonData()) ? FubiUtils.PlaybackHandID : Fubi.getHandID(i);
                if (id > 0)
                {
					if (currentIndex >= statsTree.Items.Count)
					{
						statsTree.Items.Add(new TvHand());
						((TvHand)statsTree.Items[currentIndex]).IsExpanded = true;
					}
					// Wrong type, i.e. TvUser instead of TvUHand
					if (statsTree.Items[currentIndex].GetType() != typeof(TvHand))
					{
						statsTree.Items[currentIndex] = new TvHand();
						((TvHand)statsTree.Items[currentIndex]).IsExpanded = true;
					}
					var tHand = (TvHand)statsTree.Items[currentIndex];
					tHand.id = id;

                    // Update combinations
                    var numRecs = Fubi.getNumUserDefinedCombinationRecognizers();
                    uint actualRecs = 0;
                    for (uint pc = 0; pc < numRecs; ++pc)
                    {
                        var name = Fubi.getUserDefinedCombinationRecognizerName(pc);
                        while (actualRecs >= tHand.Recs.Count)
                            tHand.Recs.Add(new TvRec());

                        var rec = tHand.Recs[(int)actualRecs];
                        rec.id = pc;
                        rec.name = name;
                        uint numStates;
                        bool isInterrupted, isInTransition;
                        rec.currState = Fubi.getCurrentCombinationRecognitionStateForHand(name, id, out numStates, out isInterrupted, out isInTransition) + 1;
                        rec.numStates = numStates;
                        rec.isInterrupted = isInterrupted;
                        rec.isInTransition = isInTransition;
                        if (HandRecognitions.ContainsKey(id) && HandRecognitions[id].ContainsKey(name) && Fubi.getCurrentTime() - HandRecognitions[id][name] < 2.0)
                            rec.bgColor = "LightGreen";
                        else
                            rec.bgColor = Color.Transparent.Name;
                        if (HandHints.ContainsKey(id) && HandHints[id].ContainsKey(name))
                            rec.hint = HandHints[id][name];

                        actualRecs++;
                    }

                    while (tHand.Recs.Count > actualRecs)
                    {
                        tHand.Recs.RemoveAt(tHand.Recs.Count - 1);
                    }

	                ++currentIndex;
                }
            }

			while (statsTree.Items.Count > currentIndex)
            {
                statsTree.Items.RemoveAt(statsTree.Items.Count - 1);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            m_running = false;
            m_updateThread.Join(1000);
        }
    }

    // Data classes for the treeview items
    public class TvUser : TreeViewItemBase
    {
        private uint m_id;
        public uint id
        {
            get 
            {
                return m_id;
            } 
            set
            { 
                if (value != m_id)
                {
                    m_id = value;
                    float r, g, b;
                    Fubi.getColorForUserID(m_id, out r, out g, out b);
                    var colorLookup = typeof(Color)
                       .GetProperties(BindingFlags.Public | BindingFlags.Static)
                       .Select(f => (Color)f.GetValue(null, null))
                       .Where(c => c.IsNamedColor)
                       .ToLookup(c => c.ToArgb());
                    var col = Color.FromArgb((int)(r * 255.0f), (int)(g * 255.0f), (int)(b * 255.0f));
                    color = colorLookup[col.ToArgb()].First().Name;
                    NotifyPropertyChanged("id");
                    NotifyPropertyChanged("color");
                } 
            }
        }

	    public string color { get; private set; }

	    private readonly ObservableCollection<TvRec> m_recs = new ObservableCollection<TvRec>();
        public ObservableCollection<TvRec> Recs { get { return m_recs; } }
    }
    public class TvHand : TreeViewItemBase
    {
        private uint m_id;
        public uint id
        {
            get
            {
                return m_id;
            }
            set
            {
                if (value != m_id)
                {
                    m_id = value;
                    float r, g, b;
                    Fubi.getColorForUserID(m_id, out r, out g, out b);
                    var colorLookup = typeof(Color)
                       .GetProperties(BindingFlags.Public | BindingFlags.Static)
                       .Select(f => (Color)f.GetValue(null, null))
                       .Where(c => c.IsNamedColor)
                       .ToLookup(c => c.ToArgb());
                    var col = Color.FromArgb((int)(r * 255.0f), (int)(g * 255.0f), (int)(b * 255.0f));
                    var namedC = colorLookup[col.ToArgb()];
	                var enumerable = namedC as Color[] ?? namedC.ToArray();
	                m_color = enumerable.Any() ? enumerable.First().Name : Color.LightGreen.Name;
                    NotifyPropertyChanged("id");
                    NotifyPropertyChanged("color");
                }
            }
        }

        private string m_color;
        public string color
        {
            get
            {
                return m_color;
            }
        }

        private readonly ObservableCollection<TvRec> m_recs = new ObservableCollection<TvRec>();
        public ObservableCollection<TvRec> Recs { get { return m_recs; } }
    }
    public class TvRec : TreeViewItemBase
    {
	    private uint m_id;
        public uint id { get { return m_id; } set { if (value != m_id) { m_id = value; NotifyPropertyChanged("id"); } } }
        private string m_name;
        public string name { get { return m_name; } set { if (value != m_name) { m_name = value; NotifyPropertyChanged("name"); } } }
        private int m_currState;
        public int currState { get { return m_currState; } set { if (value != m_currState) { m_currState = value; NotifyPropertyChanged("currState"); NotifyPropertyChanged("statColor"); NotifyPropertyChanged("progress"); } } }

        private string m_bgColor;
        public string bgColor
        {
            set
            {
                if (value != m_bgColor)
                {
                    m_bgColor = value;
                    NotifyPropertyChanged("bgColor");
                }
            }
            get
            {
                return m_bgColor;
            }
        }

        private string m_hint;
        public string hint
        {
            set
            {
                if (value != m_hint)
                {
                    m_hint = value;
                    NotifyPropertyChanged("hint");
                }
            }
            get
            {
                return m_hint;
            }
        }

        private uint m_numStates;
        public uint numStates { get { return m_numStates; } set { if (value != m_numStates) { m_numStates = value; NotifyPropertyChanged("numStates"); NotifyPropertyChanged("progress"); } } }

        private double m_progress;
        public double progress
        {
            get
            {
                if (m_currState > 0 && m_numStates > 0)
                    m_progress = m_currState / (double)m_numStates;
                else
                    m_progress = 0;
                return m_progress;
            }
            set
            {
                m_progress = value; // only a dummy setter as the progress bar needs it...
            }
        }

        private bool m_isInterrupted, m_isInTransition;
        public bool isInterrupted
        {
            set
            {
                if (value != m_isInterrupted)
                {
                    m_isInterrupted = value;
                    NotifyPropertyChanged("statusText");
                    NotifyPropertyChanged("statColor");
                }
            }
        }
        public bool isInTransition
        {
            set
            {
                if (value != m_isInTransition)
                {
                    m_isInTransition = value;
                    NotifyPropertyChanged("statusText");
                    NotifyPropertyChanged("statColor");
                }
            }
        }
        public string statusText
        {
            get
            {
                var text = "";
                if (m_isInterrupted)
                    text += "Interrupted ";
                if (m_isInTransition)
                    text += "InTransition ";
                return text;
            } 
        }

        public string statColor
        {
            get
            {
	            if (m_currState > 0)
                {
                    if (m_isInterrupted && m_isInTransition)
                        return "Purple";
                    if (m_isInterrupted)
                        return "Orange";
                    if (m_isInTransition)
                        return "Green";
                    return "Blue";
                }
	            return "Red";
            }
        }
    }
    public class TreeViewItemBase : INotifyPropertyChanged
    {
        private bool m_isSelected;
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if (value != m_isSelected)
                {
                    m_isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }
        private bool m_isExpanded;
        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                if (value != m_isExpanded)
                {
                    m_isExpanded = value;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
