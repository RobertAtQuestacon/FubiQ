using System;
using System.Diagnostics;
using FubiNET;
using System.Threading;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	class AngularMovementXmlGenerator : BasicRecognizerXmlGenerator
	{
		public AngularMovementXmlGenerator(string name, MainWindow window, XMLGenerator.RecognizerOptions options, XMLGenerator.RecognizerType type)
			: base(name, window, options, type)
		{
		}

		public override void trainValues(double start, double duration, CancellationToken ct)
		{
			waitForStart(start, ct);

			var userId = getTargetID();
			var fifthDuration = duration / 5;

			var stopwatch = Stopwatch.StartNew();

			var startAngle = recordAvgValue(stopwatch, fifthDuration, duration, userId, ct);

			while (stopwatch.Elapsed < TimeSpan.FromSeconds(duration - fifthDuration))
			{
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return;
				}
				decrementDuration(duration, stopwatch);
			}

			var endAngle = recordAvgValue(stopwatch, duration, duration, userId, ct);

			AvgValue = (endAngle - startAngle) / (duration-fifthDuration);
		}

		protected override void generateXML()
		{
			if (!Options.Local)
				appendStringAttribute(RecognizerNode, "useLocalOrientations", "false");
			if (Options.Filtered)
				appendStringAttribute(RecognizerNode, "useFilteredData", "true");

            var jointNode = Doc.CreateElement(UseHand ? "HandJoint" : "Joint", NamespaceUri);
			appendStringAttribute(jointNode, "name", getJointName(Options.SelectedJoints[0].Main));
			RecognizerNode.AppendChild(jointNode);

			var maxVelocity = Doc.CreateElement("MaxAngularVelocity", NamespaceUri);
			var minVelocity = Doc.CreateElement("MinAngularVelocity", NamespaceUri);
			if (Options.ToleranceX >= 0)
			{
				if (Options.ToleranceXType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					appendNumericAttribute(maxVelocity, "x", (AvgValue.X + Options.ToleranceX));
				if (Options.ToleranceXType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					appendNumericAttribute(minVelocity, "x", (AvgValue.X - Options.ToleranceX));
			}
			if (Options.ToleranceY >= 0)
			{
				if (Options.ToleranceYType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					appendNumericAttribute(maxVelocity, "y", (AvgValue.Y + Options.ToleranceY));
				if (Options.ToleranceYType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					appendNumericAttribute(minVelocity, "y", (AvgValue.Y - Options.ToleranceY));
			}
			if (Options.ToleranceZ >= 0)
			{
				if (Options.ToleranceZType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					appendNumericAttribute(maxVelocity, "z", (AvgValue.Z + Options.ToleranceZ));
				if (Options.ToleranceZType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					appendNumericAttribute(minVelocity, "z", (AvgValue.Z - Options.ToleranceZ));
			}
			if (maxVelocity.HasAttributes)
				RecognizerNode.AppendChild(maxVelocity);
			if (minVelocity.HasAttributes)
				RecognizerNode.AppendChild(minVelocity);
		}
	}
}
