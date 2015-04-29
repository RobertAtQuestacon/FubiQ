
namespace FubiNET
{
    public class FubiTrackingData
    {
        public FubiTrackingData()
        {
            const uint numJoints = (uint)FubiUtils.SkeletonJoint.NUM_JOINTS;
            JointOrientations = new JointOrientation[numJoints];
            for (uint i = 0; i < numJoints; ++i)
                JointOrientations[i] = new JointOrientation();
            JointPositions = new JointPosition[numJoints];
            for (uint i = 0; i < numJoints; ++i)
                JointPositions[i] = new JointPosition();
        }
        public class JointPosition
        {
            public float X, Y, Z;
            public float Confidence;
        };
        public class JointOrientation
        {
	        public float Rx, Ry, Rz;
            public float Confidence;
        };

        public float[] getArray()
        {
            const uint numJoints = (uint)FubiUtils.SkeletonJoint.NUM_JOINTS;
            var skeleton = new float[8 * numJoints];
            for (uint i = 0; i < numJoints; ++i)
            {
                uint startIndex = i * 8;
                
                skeleton[startIndex] = JointPositions[i].X;
                skeleton[startIndex + 1] = JointPositions[i].Y;
                skeleton[startIndex + 2] = JointPositions[i].Z;
                skeleton[startIndex + 3] = JointPositions[i].Confidence;

                skeleton[startIndex + 4] = JointOrientations[i].Rx;
                skeleton[startIndex + 5] = JointOrientations[i].Ry;
                skeleton[startIndex + 6] = JointOrientations[i].Rz;
                skeleton[startIndex + 7] = JointOrientations[i].Confidence;
            }
            return skeleton;
        }

        public JointOrientation[] JointOrientations;
        public JointPosition[] JointPositions;

        public double TimeStamp;
    }
}
