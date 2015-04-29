using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml;
using FubiNET;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	public class XMLGenerator
	{
		// A list of all recognizer types that can be generated/recorded
		public enum RecognizerType
		{
			JointRelation,
			JointOrientation,
			LinearMovement,
			AngularMovement,
			FingerCount,
			TemplateRecording,
			Combination
		};
		public static string getRecognizerTag(RecognizerType type)
		{
			switch (type)
			{
				case RecognizerType.JointOrientation:
					return "JointOrientationRecognizer";
				case RecognizerType.LinearMovement:
					return "LinearMovementRecognizer";
				case RecognizerType.AngularMovement:
					return "AngularMovementRecognizer";
				case RecognizerType.FingerCount:
					return "FingerCountRecognizer";
				case RecognizerType.TemplateRecording:
					return "TemplateRecognizer";
				case RecognizerType.Combination:
					return "CombinationRecognizer";
				default:
					return "JointRelationRecognizer";
			}
		}

		// All tolerance value types
		public enum ToleranceType
		{
			PlusMinus = 0,
			Lesser,
			Greater
		}
		public static string[] ToleranceTypeString =
		{
			"+-",
			"<",
			">"
		};

        // two joint structure
        public class RelativeJoint
        {
            public RelativeJoint(FubiUtils.SkeletonJoint main, FubiUtils.SkeletonJoint relative = FubiUtils.SkeletonJoint.NUM_JOINTS)
            {
                Main = main;
                Relative = relative;
            }
            public FubiUtils.SkeletonJoint Main, Relative;
        };

		// Training types for combinations
		public enum CombinationTrainingType
		{
			None,
			Gestures,
			Times,
			GesturesAndTimes
		};

		// Some events for the recognizer options windows
		public class RecognizerOptionsChangedArgs : EventArgs
		{
			public RecognizerOptionsChangedArgs(bool optionsValid)
			{
				OptionsValid = optionsValid;
			}
			public bool OptionsValid { get; set; }
		};
		public delegate void RecognizerOptionsChanged(object sender, RecognizerOptionsChangedArgs args);

		// The options for the generating the recognizer
		public RecognizerType Type;
		public string RecognizerName;
		public double Start;
		public double Duration;
		public bool StartedWithPlayback;
        public bool UseHand;

		public class RecognizerOptions
		{
            public List<RelativeJoint> SelectedJoints = new List<RelativeJoint>();
			public double ToleranceX, ToleranceY, ToleranceZ, ToleranceDist, ToleranceSpeed;
			public string ToleranceXType, ToleranceYType, ToleranceZType, ToleranceDistType, ToleranceSpeedType, ToleranceCountType;
			public uint ToleranceCount, MedianWindowSize;
			public double MaxAngleDifference;
			public bool Local, Filtered;
			public FubiUtils.BodyMeasurement MeasuringUnit;
			public double MaxDistance;
			public FubiUtils.DistanceMeasure DistanceMeasure;
			public uint IgnoreAxes;
			public bool UseOrientations, AspectInvariant;
			public string PlaybackFile;
			public int PlaybackStart, PlaybackEnd;
		};		
		public RecognizerOptions RecOptions = new RecognizerOptions();

		public class CombinationStateOptions
		{
			public List<string> Recognizers = new List<string>();
			public double MinDuration = -1;
			public double MaxDuration = -1;
			public double TimeForTransition = -1;
		};
		public class CombinationOptions
		{
			public List<CombinationStateOptions> States = new List<CombinationStateOptions>();
			public double TimeTolerance, TransitionTolerance;
            public string TimeToleranceType;
			public CombinationTrainingType TrainType;
		};
		public CombinationOptions CombOptions = new CombinationOptions();

		public void generateXML(MainWindow w, CancellationToken ct)
		{
			// Important for corrrectly converting numbers to strings
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			RecognizerXMLGenerator recognizerGen = null;
			switch (Type)
			{
				case RecognizerType.JointRelation:
				case RecognizerType.JointOrientation:
					recognizerGen = new StaticPostureXmlGenerator(RecognizerName, w, RecOptions, Type);
					break;
				case RecognizerType.LinearMovement:
					recognizerGen = new LinearMovementXmlGenerator(RecognizerName, w, RecOptions, Type);
					break;
				case RecognizerType.AngularMovement:
					recognizerGen = new AngularMovementXmlGenerator(RecognizerName, w, RecOptions, Type);
					break;
				case RecognizerType.FingerCount:
					recognizerGen = new FingerCountXmlGenerator(RecognizerName, w, RecOptions, Type);
					break;
				case RecognizerType.TemplateRecording:
					recognizerGen = new TemplateRecordingXMLGenerator(RecognizerName, w, RecOptions);
					break;
				case RecognizerType.Combination:
					recognizerGen = new CombinationXMLGenerator(RecognizerName, w, CombOptions);
					break;
			}
			if (recognizerGen != null)
			{
                recognizerGen.UseHand = UseHand;
				recognizerGen.trainValues(Start, Duration, ct);
				if (!ct.IsCancellationRequested)
				{
					var trainedFromPlaybackUser = Fubi.isPlayingSkeletonData() || Type == RecognizerType.TemplateRecording || StartedWithPlayback;
					if (recognizerGen.generateAndSaveXML() && trainedFromPlaybackUser && !String.IsNullOrEmpty(RecOptions.PlaybackFile))
					{
						// Save start and end markers of the file in case of success
						var file = new XmlDocument();
						file.Load(RecOptions.PlaybackFile);
						var currentFubiRecNode = file.GetElementsByTagName("FubiRecording")[0] as XmlElement;
						if (currentFubiRecNode != null)
						{
							currentFubiRecNode.SetAttribute("startMarker", RecOptions.PlaybackStart.ToString());
							currentFubiRecNode.SetAttribute("endMarker", RecOptions.PlaybackEnd.ToString());
							file.Save(RecOptions.PlaybackFile);
						}

					}
				}
			}

			w.Dispatcher.BeginInvoke((Action)(() =>
			{
				w.menuTabCtrl.IsEnabled = true;
			}));
		}
	}
}
