using System.Diagnostics;
using System.Xml;
using FubiNET;
using System.Threading;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	class StaticPostureXmlGenerator : BasicRecognizerXmlGenerator
	{
		public StaticPostureXmlGenerator(string name, MainWindow window, XMLGenerator.RecognizerOptions options, XMLGenerator.RecognizerType type)
			: base(name, window, options, type) { }

		public override void trainValues(double start, double duration, CancellationToken ct)
		{
			waitForStart(start, ct);

            AvgValue = recordAvgValue(Stopwatch.StartNew(), duration, duration, getTargetID(), ct);
		}

		protected override void generateXML()
		{
			if (Options.Filtered)
				appendStringAttribute(RecognizerNode, "useFilteredData", "true");

			XmlElement maxNode, minNode;
			if (Type == XMLGenerator.RecognizerType.JointRelation)
			{
				if (Options.MeasuringUnit != FubiUtils.BodyMeasurement.NUM_MEASUREMENTS)
					appendStringAttribute(RecognizerNode, "measuringUnit", FubiUtils.getBodyMeasureName(Options.MeasuringUnit));
				if (Options.Local)
					appendStringAttribute(RecognizerNode, "useLocalPositions", "true");
				var jointNode = Doc.CreateElement(UseHand ? "HandJoints" : "Joints", NamespaceUri);
				appendStringAttribute(jointNode, "main", getJointName(Options.SelectedJoints[0].Main));
				if (Options.SelectedJoints[0].Relative != FubiUtils.SkeletonJoint.NUM_JOINTS)
				{
					appendStringAttribute(jointNode, "relative", getJointName(Options.SelectedJoints[0].Relative));
				}
				RecognizerNode.AppendChild(jointNode);
				maxNode = Doc.CreateElement("MaxValues", NamespaceUri);
				minNode = Doc.CreateElement("MinValues", NamespaceUri);
			}
			else
			{
				if (!Options.Local)
					appendStringAttribute(RecognizerNode, "useLocalOrientations", "false");
                var jointNode = Doc.CreateElement(UseHand ? "HandJoint" : "Joint", NamespaceUri);
				appendStringAttribute(jointNode, "name", getJointName(Options.SelectedJoints[0].Main));
				RecognizerNode.AppendChild(jointNode);

				if (Options.MaxAngleDifference >= 0)
				{
					var orientationNode = Doc.CreateElement("Orientation", NamespaceUri);
					appendNumericAttribute(orientationNode, "x", AvgValue.X);
					appendNumericAttribute(orientationNode, "y", AvgValue.Y);
					appendNumericAttribute(orientationNode, "z", AvgValue.Z);
					appendNumericAttribute(orientationNode, "maxAngleDifference", Options.MaxAngleDifference);
					RecognizerNode.AppendChild(orientationNode);
					return;
				}
				maxNode = Doc.CreateElement("MaxDegrees", NamespaceUri);
				minNode = Doc.CreateElement("MinDegrees", NamespaceUri);
			}
			if (Options.ToleranceX >= 0)
			{
				if (Options.ToleranceXType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					appendNumericAttribute(maxNode, "x", AvgValue.X + Options.ToleranceX);
				if (Options.ToleranceXType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					appendNumericAttribute(minNode, "x", AvgValue.X - Options.ToleranceX);
			}
			if (Options.ToleranceY >= 0)
			{
				if (Options.ToleranceYType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater]) 
					appendNumericAttribute(maxNode, "y", AvgValue.Y + Options.ToleranceY);
				if (Options.ToleranceYType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					appendNumericAttribute(minNode, "y", AvgValue.Y - Options.ToleranceY);
			}
			if (Options.ToleranceZ >= 0)
			{
				if (Options.ToleranceZType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					appendNumericAttribute(maxNode, "z", AvgValue.Z + Options.ToleranceZ);
				if (Options.ToleranceZType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					appendNumericAttribute(minNode, "z", AvgValue.Z - Options.ToleranceZ);
			}
			if (Type == XMLGenerator.RecognizerType.JointRelation && Options.ToleranceDist >= 0)
			{
				var dist = AvgValue.Length;
				if (Options.ToleranceDistType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					appendNumericAttribute(maxNode, "dist", dist + Options.ToleranceDist);
				if (Options.ToleranceDistType != XMLGenerator.ToleranceTypeString[(int) XMLGenerator.ToleranceType.Lesser])
				{
					var minDist = dist - Options.ToleranceDist;
					if (minDist > 0)
						appendNumericAttribute(minNode, "dist", minDist);
				}
			}
			if (maxNode.HasAttributes)
				RecognizerNode.AppendChild(maxNode);
			if (minNode.HasAttributes) 
				RecognizerNode.AppendChild(minNode);
		}
	}
}
