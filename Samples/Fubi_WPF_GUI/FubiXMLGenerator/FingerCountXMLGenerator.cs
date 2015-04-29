using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using FubiNET;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	class FingerCountXmlGenerator : BasicRecognizerXmlGenerator
	{
		private int m_avgFingerCount;
		public FingerCountXmlGenerator(string name, MainWindow window, XMLGenerator.RecognizerOptions options, XMLGenerator.RecognizerType type)
			: base(name, window, options, type)
		{
		}

		private int recordMedianFingerCount(Stopwatch timer, double duration, double overallDuration, uint targetId, CancellationToken ct)
		{
			var leftHand = Options.SelectedJoints[0].Main == FubiUtils.SkeletonJoint.LEFT_HAND;
			var recordedFingerCounts = new List<int>();
			do
			{
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return -1;
				}

				if (UseHand)
					recordedFingerCounts.Add(Fubi.getHandFingerCount(targetId));
				else
					recordedFingerCounts.Add(Fubi.getFingerCount(targetId, leftHand, false));
				decrementDuration(overallDuration, timer);
				// With 10 ms sleep we should still catch all different values, but we don't get too many
				Thread.Sleep(10);
			} while (timer.Elapsed < TimeSpan.FromSeconds(duration));

			recordedFingerCounts.Sort();
			return recordedFingerCounts[recordedFingerCounts.Count / 2];
		}

		public override void trainValues(double start, double duration, CancellationToken ct)
		{
			waitForStart(start, ct);

            m_avgFingerCount = recordMedianFingerCount(Stopwatch.StartNew(), duration, duration, getTargetID(), ct);
		}

		protected override void generateXML()
		{
			if (Options.Filtered)
				appendStringAttribute(RecognizerNode, "useFilteredData", "true");

			if (Options.SelectedJoints.Count == 1)
			{
                var jointNode = Doc.CreateElement(UseHand ? "HandJoint" : "Joint", NamespaceUri);
				appendStringAttribute(jointNode, "name", getJointName(Options.SelectedJoints[0].Main));
				RecognizerNode.AppendChild(jointNode);
			}

			var fingerCountNode = Doc.CreateElement("FingerCount", NamespaceUri);
			if (Options.ToleranceCountType != XMLGenerator.ToleranceTypeString[(int) XMLGenerator.ToleranceType.Lesser])
				appendNumericAttribute(fingerCountNode, "min", Math.Max(0, m_avgFingerCount - Options.ToleranceCount));
			if (Options.ToleranceCountType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
				appendNumericAttribute(fingerCountNode, "max", Math.Min(5, m_avgFingerCount + Options.ToleranceCount));
			if (Options.MedianWindowSize > 0)
			{
				appendStringAttribute(fingerCountNode, "useMedianCalculation", "true");
				appendNumericAttribute(fingerCountNode, "medianWindowSize", Options.MedianWindowSize, 0);
			}
			RecognizerNode.AppendChild(fingerCountNode);
		}
	}
}
