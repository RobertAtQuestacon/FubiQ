using FubiNET;
using System.Threading;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	class TemplateRecordingXMLGenerator : BasicRecognizerXmlGenerator
	{
		public TemplateRecordingXMLGenerator(string name, MainWindow window, XMLGenerator.RecognizerOptions options)
			: base(name, window, options, XMLGenerator.RecognizerType.TemplateRecording)
		{
		}

		public override void trainValues(double start, double duration, CancellationToken ct)
		{
			// Nothing to train here
		}

		protected override void generateXML()
		{
			if (Options.Filtered)
				appendStringAttribute(RecognizerNode, "useFilteredData", "true");
			if (Options.MeasuringUnit != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
				appendStringAttribute(RecognizerNode, "measuringUnit", FubiUtils.getBodyMeasureName(Options.MeasuringUnit));
			if (Options.Local)
				appendStringAttribute(RecognizerNode, "useLocalTransformations", "true");

			if (Options.MaxAngleDifference >= 0)
				appendNumericAttribute(RecognizerNode, "maxRotation", Options.MaxAngleDifference, 3);
			appendNumericAttribute(RecognizerNode, "maxDistance", Options.MaxDistance, 3);
			appendStringAttribute(RecognizerNode, "distanceMeasure", Options.DistanceMeasure.ToString().ToLower());
			if (Options.UseOrientations)
				appendStringAttribute(RecognizerNode, "useOrientations", "true");
			if (Options.AspectInvariant)
				appendStringAttribute(RecognizerNode, "aspectInvariant", "true");

            foreach (XMLGenerator.RelativeJoint j in Options.SelectedJoints)
            {
                var jointNode = Doc.CreateElement(UseHand ? "HandJoints" : "Joints", NamespaceUri);
                appendStringAttribute(jointNode, "main", getJointName(j.Main));
                if (j.Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
                {
                    appendStringAttribute(jointNode, "relative", getJointName(j.Relative));
                }
                RecognizerNode.AppendChild(jointNode);
            }

			var trainingNode = Doc.CreateElement("TrainingData", NamespaceUri);
			appendStringAttribute(trainingNode, "file", Options.PlaybackFile);
			appendNumericAttribute(trainingNode, "start", Options.PlaybackStart, 0);
			appendNumericAttribute(trainingNode, "end", Options.PlaybackEnd, 0);
			RecognizerNode.AppendChild(trainingNode);

			if (Options.IgnoreAxes != 0)
			{
				var ignoreAxesNode = Doc.CreateElement("IgnoreAxes", NamespaceUri);
				if ((Options.IgnoreAxes & (uint)FubiUtils.CoordinateAxis.X) != 0)
					appendStringAttribute(ignoreAxesNode, "x", "true");
				if ((Options.IgnoreAxes & (uint)FubiUtils.CoordinateAxis.Y) != 0)
					appendStringAttribute(ignoreAxesNode, "y", "true");
				if ((Options.IgnoreAxes & (uint)FubiUtils.CoordinateAxis.Z) != 0)
					appendStringAttribute(ignoreAxesNode, "z", "true");
				RecognizerNode.AppendChild(ignoreAxesNode);
			}
		}
	}
}
