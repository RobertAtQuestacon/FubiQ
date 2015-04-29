using System;
using System.Diagnostics;
using FubiNET;
using System.Windows.Media.Media3D;
using System.Threading;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	class LinearMovementXmlGenerator : BasicRecognizerXmlGenerator
	{
		double m_speed;
		private float m_measureDist;
		
		public LinearMovementXmlGenerator(string name, MainWindow window, XMLGenerator.RecognizerOptions options, XMLGenerator.RecognizerType type)
			: base(name, window, options, type)
		{}


		public override void trainValues(double start, double duration, CancellationToken ct)
		{
			waitForStart(start, ct);

            var userId = getTargetID();
			var fifthDuration = duration / 5;

			var stopwatch = Stopwatch.StartNew();

			// Record avg pos for first fifth duration
			var startPos = recordAvgValue(stopwatch, fifthDuration, duration, userId, ct);
			
			while (stopwatch.Elapsed < TimeSpan.FromSeconds(duration - fifthDuration))
			{
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return;
				}
				decrementDuration(duration, stopwatch);
			}

			// Record avg pos for last fifth duration
			var endPos = recordAvgValue(stopwatch, duration, duration, userId, ct);

			// Record measurement distance
			if (Options.MeasuringUnit != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
			{
				float measureConfidence;
				Fubi.getBodyMeasurementDistance(userId, Options.MeasuringUnit, out m_measureDist, out measureConfidence);
			}

			// Calc change between first and last fifth avg position
			AvgValue = endPos - startPos;

			// Apply active axes
			if ((Options.IgnoreAxes & (uint)FubiUtils.CoordinateAxis.X) != 0)
				AvgValue.X = 0;
			if ((Options.IgnoreAxes & (uint)FubiUtils.CoordinateAxis.Y) != 0)
				AvgValue.Y = 0;
			if ((Options.IgnoreAxes & (uint)FubiUtils.CoordinateAxis.Z) != 0)
				AvgValue.Z = 0;

			// Calculate speed
			m_speed = (endPos - startPos).Length / (duration - fifthDuration);
		}

		protected override void generateXML()
		{
			if (Options.Local)
				appendStringAttribute(RecognizerNode, "useLocalPositions", "true");
			if (Options.Filtered)
				appendStringAttribute(RecognizerNode, "useFilteredData", "true");
			if (Options.MeasuringUnit != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
				appendStringAttribute(RecognizerNode, "measuringUnit", FubiUtils.getBodyMeasureName(Options.MeasuringUnit));

            var jointNode = Doc.CreateElement(UseHand ? "HandJoints" : "Joints", NamespaceUri);
			appendStringAttribute(jointNode, "main", getJointName(Options.SelectedJoints[0].Main));
			if (Options.SelectedJoints[0].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
			{
				appendStringAttribute(jointNode, "relative", getJointName(Options.SelectedJoints[0].Relative));
			}
			RecognizerNode.AppendChild(jointNode);

            Vector3D direction = AvgValue;
            double length = AvgValue.Length;
            if (length > Double.Epsilon)
                direction.Normalize();
            else // No valid direction recorded, so we arbitrarily chose up direction
                direction = new Vector3D(0, 1, 0);
			var directionNode = Doc.CreateElement("Direction", NamespaceUri);
            appendNumericAttribute(directionNode, "x", direction.X, 3);
            appendNumericAttribute(directionNode, "y", direction.Y, 3);
            appendNumericAttribute(directionNode, "z", direction.Z, 3);
			if (Options.MaxAngleDifference >= 0)
				appendNumericAttribute(directionNode, "maxAngleDifference", Options.MaxAngleDifference, 3);
			RecognizerNode.AppendChild(directionNode);

            if (Options.ToleranceDist >= 0)
            {
				if (Options.MeasuringUnit != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
				{
					if (m_measureDist > 0)
					{
						AvgValue.X /= m_measureDist;
						AvgValue.Y /= m_measureDist;
						AvgValue.Z /= m_measureDist;
						length = AvgValue.Length;
					}
				}
                var lengthNode = Doc.CreateElement("Length", NamespaceUri);
                if (length - Options.ToleranceDist > 0 && Options.ToleranceDistType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
                    appendNumericAttribute(lengthNode, "min", length - Options.ToleranceDist, 3);
                if (Options.ToleranceDistType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
                    appendNumericAttribute(lengthNode, "max", length + Options.ToleranceDist, 3);
                if (lengthNode.HasAttributes)
                    RecognizerNode.AppendChild(lengthNode);
            }
            if (Options.ToleranceSpeed >= 0)
            {
                var speedNode = Doc.CreateElement("Speed", NamespaceUri);
                if (m_speed - Options.ToleranceSpeed > 0 && Options.ToleranceSpeedType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
                    appendNumericAttribute(speedNode, "min", m_speed - Options.ToleranceSpeed, 3);
                if (Options.ToleranceSpeedType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
                    appendNumericAttribute(speedNode, "max", m_speed + Options.ToleranceSpeed, 3);
                if (speedNode.HasAttributes)
                    RecognizerNode.AppendChild(speedNode);
            }
		}
	}
}
