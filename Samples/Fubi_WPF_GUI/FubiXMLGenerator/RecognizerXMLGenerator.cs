using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml;
using FubiNET;
using Fubi_WPF_GUI.Properties;
using Microsoft.Win32;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	abstract class RecognizerXMLGenerator
	{
		protected XmlDocument Doc;
		protected XmlElement RecognizerNode;
		protected XMLGenerator.RecognizerType Type;
		protected const string NamespaceUri = "http://www.hcm-lab.de";

		protected string GestureName;

		protected MainWindow MainWindow;

        public bool UseHand;

		protected RecognizerXMLGenerator(string name, MainWindow window, XMLGenerator.RecognizerType recType)
		{
			GestureName = name;
			Type = recType;
			Doc = new XmlDocument();
			MainWindow = window;
		}

		public bool generateAndSaveXML()
		{
			var ret = false;
			setDescription("Finished - Generating XML..");
			initXML();
			generateXML();
			var res = MessageBox.Show(RecognizerNode.OuterXml + "\n\nDo you want to save the XML?", "Generated XML", MessageBoxButton.YesNo);
			if (res == MessageBoxResult.Yes)
				ret = openSaveDialog();
			setDescription("Select options and start recording.");
			return ret;
		}

		// Abstract functions to be implemented by all subclasses
		public abstract void trainValues(double start, double duration, CancellationToken ct);
		protected abstract void generateXML();

		// Communication with the GUI of the main window
		public void setTimer(double time)
		{
			MainWindow.Dispatcher.BeginInvoke((Action)(() =>
			{
				MainWindow.xmlGenCountDownLabel.Content = time.ToString("F2");
			}));
		}
		public void setDescription(string desc)
		{
			MainWindow.Dispatcher.BeginInvoke((Action)(() =>
			{
				MainWindow.xmlGenDescription.Content = desc;
			}));
		}
		public void setGestureLabel(string gest)
		{
			MainWindow.Dispatcher.BeginInvoke((Action)(() =>
			{
				if (gest != null && gest.Trim() != "")
					MainWindow.xmlGenGestureLabel.Content = "Current Gesture: " + gest;
				else
					MainWindow.xmlGenGestureLabel.Content = "";
			}));
		}
		protected void decrementDuration(double startDuration, Stopwatch timer)
		{
			var seconds = startDuration * 1000 - timer.ElapsedMilliseconds;
			seconds = Math.Round(seconds / 1000, 1);
			setTimer(seconds);
		}

		// waiting before recording
		protected void waitForStart(double startTime, CancellationToken ct)
		{
			setDescription("Starting - Get Ready!");
			var timer = new Stopwatch();
			timer.Start();
			var duration = TimeSpan.FromSeconds(startTime);
			while (timer.Elapsed < duration)
			{
				MainWindow.Dispatcher.BeginInvoke((Action)(() => decrementDuration(startTime, timer)));
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return;
				}
			}
			setDescription("Recording..");
		}

		// Generate a valid XML for the Fubi recognizers
		protected void initXML()
		{
			var docNode = Doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
			Doc.AppendChild(docNode);

			var fubiRec = Doc.CreateElement("FubiRecognizers", NamespaceUri);

			appendNumericAttribute(fubiRec, "globalMinConfidence", 0.51);
			appendStringAttribute(fubiRec, "xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			appendStringAttribute(fubiRec, "xsi:schemaLocation", "http://www.hcm-lab.de http://www.hcm-lab.de/downloads/FubiRecognizers.xsd");

			RecognizerNode = Doc.CreateElement(XMLGenerator.getRecognizerTag(Type), NamespaceUri);
			appendStringAttribute(RecognizerNode, "name", GestureName);
			fubiRec.AppendChild(RecognizerNode);

			Doc.AppendChild(fubiRec);
		}

		protected XmlNode findNode(XmlNode parentNode, string name, bool onlyBasicRecognizers = false)
		{
			Debug.Assert(parentNode.OwnerDocument != null, "parentNode.OwnerDocument != null");
			Debug.Assert(parentNode.OwnerDocument.DocumentElement != null, "parentNode.OwnerDocument.DocumentElement != null");

			var xnm = new XmlNamespaceManager(parentNode.OwnerDocument.NameTable);
			xnm.AddNamespace("temp", parentNode.OwnerDocument.DocumentElement.NamespaceURI);
			var node = ((((parentNode.SelectSingleNode("//temp:JointRelationRecognizer[@name='" + name + "']", xnm) ??
						 parentNode.SelectSingleNode("//temp:JointOrientationRecognizer[@name='" + name + "']", xnm)) ??
						parentNode.SelectSingleNode("//temp:LinearMovementRecognizer[@name='" + name + "']", xnm)) ??
						parentNode.SelectSingleNode("//temp:TemplateRecognizer[@name='" + name + "']", xnm)) ??
					   parentNode.SelectSingleNode("//temp:AngularMovementRecognizer[@name='" + name + "']", xnm)) ??
					   parentNode.SelectSingleNode("//temp:FingerCountRecognizer[@name='" + name + "']", xnm);
			if (node == null && !onlyBasicRecognizers)
				node = parentNode.SelectSingleNode("//temp:CombinationRecognizer[@name='" + name + "']", xnm);			
			return node;
		}

		protected void appendNumericAttribute(XmlElement parentNode, string name, double value, int digits = 2)
		{
			var attribute = Doc.CreateAttribute(name);
			attribute.Value = Math.Round(value, digits).ToString();
			parentNode.Attributes.Append(attribute);
		}

		protected void appendStringAttribute(XmlElement parentNode, string name, string value)
		{
			var attribute = Doc.CreateAttribute(name);
			attribute.Value = value;
			parentNode.Attributes.Append(attribute);
		}

		protected bool openSaveDialog()
		{
			var ret = false;
			var sfd = new SaveFileDialog
			{
				FileName = Path.GetFileName(Settings.Default.LastGenerateXMLPath),
				InitialDirectory = Path.GetDirectoryName(Settings.Default.LastGenerateXMLPath),
				DefaultExt = ".xml",
				AddExtension = true,
				Filter = "Xml documents (.xml)|*.xml",
				OverwritePrompt = false
			};
			var result = sfd.ShowDialog();
			if (result == true)
			{
				ret = saveXMLFile(sfd.FileName);
				Settings.Default.LastGenerateXMLPath = sfd.FileName;
				Settings.Default.Save();
			}
			return ret;
		}

		protected bool saveXMLFile(string path)
		{
			var ret = false;
			var file = new XmlDocument();
			try
			{
				file.Load(path);
				var currentFubiRecNode = file.GetElementsByTagName("FubiRecognizers")[0];
				if (currentFubiRecNode == null)
				{
					var res = MessageBox.Show("You have chosen an invalid formatted xml file. \nDo you want to overwrite it?", "Invalid file format", MessageBoxButton.YesNo, MessageBoxImage.Warning);
					if (res == MessageBoxResult.Yes) // Overwrite
					{
						Doc.Save(path);
					}
					else // Try again...
					{
						return openSaveDialog();
					}
				}
				var importedRecNode = file.ImportNode(RecognizerNode, true);
				var recNodeWithSameName = findNode(currentFubiRecNode, GestureName);
				if (recNodeWithSameName == null)
				{
					Debug.Assert(currentFubiRecNode != null, "currentFubiRecNode != null");
					currentFubiRecNode.AppendChild(importedRecNode);
				}
				else
				{
					var res = MessageBox.Show("There is a recognizer with the same name present. \nWould you like to replace it?", "Same recognizer present", MessageBoxButton.YesNo, MessageBoxImage.Warning);
					if (res == MessageBoxResult.Yes)
					{
						Debug.Assert(recNodeWithSameName.ParentNode != null, "recNodeWithSameName.ParentNode != null");
						recNodeWithSameName.ParentNode.ReplaceChild(importedRecNode, recNodeWithSameName);
					}
					else
					{
						int c;
						var newName = GestureName;
						for (c = 1; recNodeWithSameName != null; c++)
						{
							newName = GestureName + c;
							recNodeWithSameName = findNode(currentFubiRecNode, newName);
						}
						Debug.Assert(importedRecNode.Attributes != null, "importedRecNode.Attributes != null");
						var nameAttr = importedRecNode.Attributes["name"];
						if (nameAttr != null)
							nameAttr.Value = newName;
						Debug.Assert(currentFubiRecNode != null, "currentFubiRecNode != null");
						currentFubiRecNode.AppendChild(importedRecNode);
					}
				}
				file.Save(path);
				ret = true;
			}
			catch (FileNotFoundException)
			{
				try
				{
					Doc.Save(path);
				}
				catch (Exception e)
				{
					MessageBox.Show("Error creating file: " + e.Message, "Error Creating File", MessageBoxButton.OK, MessageBoxImage.Warning);
					// Try again
					return openSaveDialog();
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Error Writing to file: " + e.Message, "Error Writing to File", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Try again
				return openSaveDialog();
			}
			Process.Start(path);
			return ret;
		}

        private uint getPlaybackOrClosestUserID()
		{
			if (Fubi.isPlayingSkeletonData())
				return FubiUtils.PlaybackUserID;
			return Fubi.getClosestUserID();
		}
        private uint getPlaybackOrClosestHandID()
        {
            if (Fubi.isPlayingSkeletonData())
                return FubiUtils.PlaybackHandID;
            return Fubi.getClosestHandID();
        }
        protected uint getTargetID()
        {
            return UseHand ? getPlaybackOrClosestHandID() : getPlaybackOrClosestUserID();
        }
        protected string getJointName(FubiUtils.SkeletonJoint jointID)
        {
            return UseHand ? FubiUtils.getHandJointName((FubiUtils.SkeletonHandJoint)jointID) : FubiUtils.getJointName(jointID);
        }
	}

	// subclass with common functionality for all basic recognizers (jointRelation, jointOrienation, angularMovement, linearMovement)
	abstract class BasicRecognizerXmlGenerator : RecognizerXMLGenerator
	{
		protected XMLGenerator.RecognizerOptions Options;
        protected List<Vector3D> RecordedJointValues = new List<Vector3D>();
		protected Vector3D RecordedMeasurementValue = new Vector3D();
		protected Vector3D AvgValue = new Vector3D();

		protected BasicRecognizerXmlGenerator(string name, MainWindow window, XMLGenerator.RecognizerOptions recOptions, XMLGenerator.RecognizerType recType)
			: base(name, window, recType)
		{
			Options = recOptions;
            RecordedJointValues.Add(new Vector3D());
            if (Options.SelectedJoints[0].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
                RecordedJointValues.Add(new Vector3D());
		}

		protected Vector3D getCurrentJointValue(uint targetID, FubiUtils.SkeletonJoint jointID, FubiUtils.BodyMeasurement measureID = FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
		{
			float x, y, z, confidence;
			double timeStamp;

			if (Type == XMLGenerator.RecognizerType.JointRelation || Type == XMLGenerator.RecognizerType.LinearMovement)
			{
				if (UseHand)
					Fubi.getCurrentHandJointPosition(targetID, (FubiUtils.SkeletonHandJoint)jointID, out x, out y, out z, out confidence, out timeStamp, Options.Local, Options.Filtered);
				else
				{
					Fubi.getCurrentSkeletonJointPosition(targetID, jointID, out x, out y, out z, out confidence, out timeStamp, Options.Local, Options.Filtered);
					if (measureID != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS && Type == XMLGenerator.RecognizerType.JointRelation)
					{
						float measureDist, measureConfidence;
						Fubi.getBodyMeasurementDistance(targetID, measureID, out measureDist, out measureConfidence);
						if (measureDist > 0)
						{
							x /= measureDist;
							y /= measureDist;
							z /= measureDist;
						}
					}
				}
			}
			else
			{
				var mat = new float[9];
				if (UseHand)
					Fubi.getCurrentHandJointOrientation(targetID, (FubiUtils.SkeletonHandJoint)jointID, mat, out confidence, out timeStamp, Options.Local, Options.Filtered);
				else
					Fubi.getCurrentSkeletonJointOrientation(targetID, jointID, mat, out confidence, out timeStamp, Options.Local, Options.Filtered);
				FubiUtils.Math.rotMatToRotation(mat, out x, out y, out z);
			}
			return new Vector3D(x, y, z);
		}

		protected Vector3D recordAvgValue(Stopwatch timer, double duration, double overallDuration, uint targetId, CancellationToken ct)
		{
			var counter = 0;
			do
			{
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return new Vector3D();
				}
				counter++;
				RecordedJointValues[0] += getCurrentJointValue(targetId, Options.SelectedJoints[0].Main, Options.MeasuringUnit);
                if (Options.SelectedJoints[0].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
					RecordedJointValues[1] += getCurrentJointValue(targetId, Options.SelectedJoints[0].Relative, Options.MeasuringUnit);
				decrementDuration(overallDuration, timer);
				// With 10 ms sleep we should still catch all different values, but we don't get too many
				Thread.Sleep(10);
			} while (timer.Elapsed < TimeSpan.FromSeconds(duration));


			var value = RecordedJointValues[0] / counter;
            if (Options.SelectedJoints[0].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
			{
				value -= RecordedJointValues[1] / counter;
			}

			return value;
		}
	};
}
