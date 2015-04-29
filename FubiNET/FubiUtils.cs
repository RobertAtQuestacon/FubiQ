using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace FubiNET
{
	public class FubiUtils
	{
		// Options for image rendering
		public enum ImageType
		{
			/*	The possible image types
			*/
			Color = 0,
			Depth,
			IR,
			Blank
		};
		public enum ImageNumChannels
		{
			/*	The number of channels in the image
			*/
			C1 = 1,
			C3 = 3,
			C4 = 4
		};
		public enum ImageDepth
		{
			/*	The depth of each channel
			*/
			D8 = 8,
			D16 = 16,
			F32 = 32
		};
		public enum DepthImageModification
		{
			/*	How the depth image should be modified for depth differences
				being easier to distinguish by the human eye
			*/
			Raw = 0,
			UseHistogram,
			StretchValueRange,
			ConvertToRGB
		};
		public enum RenderOptions
		{
			/*	The possible formats for the tracking info rendering
			*/
			None = 0,
			Shapes =                0x000001,
			Skeletons =             0x000002,
			UserCaptions =          0x000004,
			LocalOrientCaptions =   0x000008,
			GlobalOrientCaptions =  0x000010,
			LocalPosCaptions =      0x000020,
			GlobalPosCaptions =     0x000040,
			Background =            0x000080,
			SwapRAndB =             0x000100,
			FingerShapes =          0x000200,
			DetailedFaceShapes =    0x000400,
			BodyMeasurements =      0x000800,
			UseFilteredValues =     0x001000,
			Default = Shapes | Skeletons | UserCaptions
		};
		
		public enum JointsToRender
		{
			/* IDs for the Joints to define which of them should be rendered (don't mix up with the SkeletonJoint enum!!) */
			ALL_JOINTS = -1,

			HEAD = 0x00000001,
			NECK = 0x00000002,
			TORSO = 0x00000004,
			WAIST = 0x00000008,

			LEFT_SHOULDER = 0x00000010,
			LEFT_ELBOW = 0x00000020,
			LEFT_WRIST = 0x00000040,
			LEFT_HAND = 0x00000080,

			RIGHT_SHOULDER = 0x00000100,
			RIGHT_ELBOW = 0x00000200,
			RIGHT_WRIST = 0x00000400,
			RIGHT_HAND = 0x00000800,

			LEFT_HIP = 0x00001000,
			LEFT_KNEE = 0x00002000,
			LEFT_ANKLE = 0x00004000,
			LEFT_FOOT = 0x00008000,

			RIGHT_HIP = 0x00010000,
			RIGHT_KNEE = 0x00020000,
			RIGHT_ANKLE = 0x00040000,
			RIGHT_FOOT = 0x00080000,

			FACE_NOSE = 0x00100000,
			FACE_LEFT_EAR = 0x00200000,
			FACE_RIGHT_EAR = 0x00400000,
			FACE_FOREHEAD = 0x00800000,
			FACE_CHIN = 0x01000000,

			PALM = 0x02000000,
			THUMB = 0x04000000,
			INDEX = 0x08000000,
			MIDDLE = 0x10000000,
			RING = 0x20000000,
			PINKY = 0x40000000
		};

		// Maximum depth value that can occure in the depth image
		public const int MaxDepth = 10000;
		// Maximum number of tracked users
		public const int MaxUsers = 15;
        /**
	    * \brief UserID for the playback of recorded skeleton data
	    */
		public const int PlaybackUserID = MaxUsers + 1;
		/**
		* \brief And hands
		*/
		public const int MaxHands = 2*MaxUsers;
		/**
		* \brief HandID for the playback of recorded skeleton data
		* As the hand ids have no fixed limit we need take max int to be on the safe side...
		*/
		public const int PlaybackHandID = Math.MaxInt32;

		public enum SkeletonJoint
		{
			HEAD			= 0,
			NECK			= 1,
			TORSO			= 2,
			WAIST			= 3,

			LEFT_SHOULDER	= 4,
			LEFT_ELBOW		= 5,
			LEFT_WRIST		= 6,
			LEFT_HAND		= 7,

			RIGHT_SHOULDER	=8,
			RIGHT_ELBOW		=9,
			RIGHT_WRIST		=10,
			RIGHT_HAND		=11,

			LEFT_HIP		=12,
			LEFT_KNEE		=13,
			LEFT_ANKLE		=14,
			LEFT_FOOT		=15,

			RIGHT_HIP		=16,
			RIGHT_KNEE		=17,
			RIGHT_ANKLE		=18,
			RIGHT_FOOT		=19,

			FACE_NOSE       =20,
			FACE_LEFT_EAR   =21,
			FACE_RIGHT_EAR  =22,
			FACE_FOREHEAD   =23,
			FACE_CHIN       =24,

			NUM_JOINTS		=25
		};

		/**
		* \brief IDs for all supported finger tracking skeleton joints
		*/
		public enum SkeletonHandJoint
		{
			PALM = 0,
			THUMB = 1,
			INDEX = 2,
			MIDDLE = 3,
			RING = 4,
			PINKY = 5,

			NUM_JOINTS = 6
		};

		public enum BodyMeasurement
		{
			BODY_HEIGHT = 0,
			TORSO_HEIGHT = 1,
			SHOULDER_WIDTH = 2,
			HIP_WIDTH = 3,
			ARM_LENGTH = 4,
			UPPER_ARM_LENGTH = 5,
			LOWER_ARM_LENGTH = 6,
			LEG_LENGTH = 7,
			UPPER_LEG_LENGTH = 8,
			LOWER_LEG_LENGTH = 9,
			NUM_MEASUREMENTS = 10
		};

		public enum SkeletonProfile
		{
			NONE = 1,

			ALL = 2,

			UPPER_BODY = 3,

			LOWER_BODY = 4,

			HEAD_HANDS = 5,
		};

		public enum SensorType
		{
			/** No sensor in use **/
			NONE = 0,
			/** Sensor based on OpenNI 2.x**/
			OPENNI2 = 0x01,
			/** Sensor based on OpenNI 1.x**/
			OPENNI1 = 0x02,
			/** Sensor based on the Kinect for Windows SDK 1.x**/
            KINECTSDK = 0x04,
            /** Sensor based on the Kinect for Windows SDK 2.x**/
            KINECTSDK2 = 0x08
		};

		public enum FingerSensorType
		{
			/** No sensor in use **/
			NONE = 0x0,
			/** Finger tracking with LEAP **/
			LEAP = 0x01
		};

		/**
		* \brief A recognizer can target a specific type of sensor that provides the correct data
		*/
		public enum RecognizerTarget
		{
			/** No target, usually means combination recognizer that is not yet initialized or something went wrong **/
			INVALID = -1,
			/** A full body skeleton as provided by the Kinect **/
			BODY = 0,
			/** A finger skeleton as provided by the leap motion sensor or finger counts and targeting left hands **/
			LEFT_HAND = 1,
			/** A finger skeleton as provided by the leap motion sensor or finger counts and targeting right hands **/
			RIGHT_HAND = 2,
			/** A finger skeleton as provided by the leap motion sensor or finger counts and targeting any hand **/
			BOTH_HANDS = 3,
			/** Combinaiton recognizer that includes recongizers that refer both types of skeletons **/
			BODY_AND_HANDS = 4
		};

		/**
		* \brief A recognizer can be either user-defined or not and basic or a combination
		*/
		public enum RecognizerType
		{
			USERDEFINED_GESTURE = 0,
			USERDEFINED_COMBINATION = 1,
			PREDEFINED_GESTURE = 2,
			PREDEFINED_COMBINATION = 3,
			NUM_TYPES
		};

		public enum RecognitionResult
		{
			/*	Result of a gesture recognition
			*/
			TRACKING_ERROR = -1,
			NOT_RECOGNIZED = 0,
			RECOGNIZED = 1,
			WAITING_FOR_LAST_STATE_TO_FINISH = 2	// Only for combinations with waitUntilLastStateRecognizersStop flag
		};

		public enum CoordinateType
		{
			/**
			* \brief Additional information about what went wrong and how to correct it
			*/
			COLOR = 0,
			DEPTH,
			IR,
			REAL_WORLD
		};

		public enum DistanceMeasure
		{
			/**
			* \brief Different kinds of distance measures, e.g. used in dtw function
			*/
			Manhattan = 0,
			Euclidean = 1,
			Malhanobis = 2
		};

		public enum CoordinateAxis
		{
			/**
			* \brief Coordinate axis flags that can be combined in one paramter
			*/
			NONE = 0x0,
			X = 0x1,
			Y = 0x2,
			Z = 0x4
		};

		public enum StochasticModel
		{
			/**
			* \brief Stochastic models to be used in template recognizers
			*/
			NONE = 0x0,
			HMM = 0x1,
			GMR = 0x2
		};

		[StructLayout(LayoutKind.Sequential)]
		public class RecognitionCorrectionHint
		{
			/**
				* \brief Additional information about what went wrong and how to correct it
			*/
			public enum ChangeType
			{
				SPEED,
				POSE,
				DIRECTION,
				FORM,
				FINGERS
			};
			public enum ChangeDirection
			{
				DIFFERENT,
				MORE,
				LESS
			};

			[MarshalAs(UnmanagedType.I4)]
			public SkeletonJoint m_joint;
			[MarshalAs(UnmanagedType.R4)]
			public float m_dirX;
			[MarshalAs(UnmanagedType.R4)]
			public float m_dirY;
			[MarshalAs(UnmanagedType.R4)]
			public float m_dirZ;
			[MarshalAs(UnmanagedType.R4)]
			public float m_dist;
			[MarshalAs(UnmanagedType.U1)]
			public bool m_isAngle;
			[MarshalAs(UnmanagedType.I4)]
			public ChangeType m_changeType;
			[MarshalAs(UnmanagedType.I4)]
			public ChangeDirection m_changeDirection;
			[MarshalAs(UnmanagedType.I4)]
			public BodyMeasurement m_measuringUnit;
			[MarshalAs(UnmanagedType.I4)]
			public int m_failedState;

			public RecognitionCorrectionHint(SkeletonJoint joint = SkeletonJoint.NUM_JOINTS, float dirX = 0, float dirY = 0, float dirZ = 0, float dist = 0,
				bool isAngle = false, ChangeType changeType = ChangeType.SPEED, ChangeDirection changeDir = ChangeDirection.DIFFERENT,
				BodyMeasurement measuringUnit = BodyMeasurement.NUM_MEASUREMENTS, int failedState = -1)
			{
				m_joint = joint;
				m_dirX = dirX;
				m_dirY = dirY;
				m_dirZ = dirZ;
				m_dist = dist;
				m_isAngle = isAngle;
				m_changeType = changeType;
				m_changeDirection = changeDir;
				m_measuringUnit = measuringUnit;
				m_failedState = failedState;
			}
		};

		public class StreamOptions
		{
			public StreamOptions(int width = 640, int height = 480, int fps = 30)
			{
				Width =width;
				Height =height;
				Fps = fps;
			}
			public void invalidate()
			{
				Width = -1; Height = -1; Fps = -1;
			}
			public bool isValid()
			{
				return Width > 0 && Height > 0 && Fps > 0;
			}

			public int Width;
			public int Height;
			public int Fps;
		};

		public class FilterOptions
		{
			public FilterOptions(float filterMinCutOffFrequency = 1.0f, 
				float filterVelocityCutOffFrequency = 1.0f, float filterCutOffSlope = 0.007f)
			{
				FilterMinCutOffFrequency = filterMinCutOffFrequency;
				FilterVelocityCutOffFrequency = filterVelocityCutOffFrequency;
				FilterCutOffSlope = filterCutOffSlope;
			}

			
			public float FilterMinCutOffFrequency;
			public float FilterVelocityCutOffFrequency;
			public float FilterCutOffSlope;
		};

		public class SensorOptions
		{
			public SensorOptions(StreamOptions depthOptions,
				StreamOptions rgbOptions, StreamOptions irOptions,
				SensorType sensorType = SensorType.OPENNI2,
				SkeletonProfile trackingProfile = SkeletonProfile.ALL,
				bool mirrorStreams = true, bool registerStreams = true)
			{
				DepthOptions = depthOptions;
				IROptions = irOptions;
				RGBOptions = rgbOptions;
				TrackingProfile = trackingProfile;
				MirrorStreams = mirrorStreams;
				RegisterStreams = registerStreams;
				Type = sensorType;
			}
			public StreamOptions DepthOptions;
			public StreamOptions IROptions;
			public StreamOptions RGBOptions;

			public SkeletonProfile TrackingProfile;
			public bool MirrorStreams;
			public bool RegisterStreams;
			public SensorType Type;
		};


		// Constants
		public class Math
		{
			public const UInt32 MaxUInt32 = 0xFFFFFFFF;
			public const int MinInt32 = 0x8000000;
			public const int MaxInt32 = 0x7FFFFFFF;
			public const float MaxFloat = 3.402823466e+38F;
			public const float MinPosFloat = 1.175494351e-38F;

			public const float Pi = 3.141592654f;
			public const float TwoPi = 6.283185307f;
			public const float PiHalf = 1.570796327f;

			public const float Epsilon = 0.000001f;
			public const float ZeroEpsilon = 32.0f * MinPosFloat;  // Very small epsilon for checking against 0.0f

			public const float NaN = 0xFFFFFFFF;

			public static float degToRad( float f ) 
			{
				return f * 0.017453293f;
			}

			public static float radToDeg(float f) 
			{
				return f * 57.29577951f;
			}

			public static bool rotMatToRotation(float[] mat, out float rx, out float ry, out float rz)
			{
				if (mat.Length == 9)
				{
					rx = radToDeg((float)System.Math.Asin(-mat[7]));

					// Special case: Cos[x] == 0 (when Sin[x] is +/-1)
					float f = System.Math.Abs(mat[7]);
					if (f > 0.999f && f < 1.001f)
					{
						// Pin arbitrarily one of y or z to zero
						// Mathematical equivalent of gimbal lock
						ry = 0;

						// Now: Cos[x] = 0, Sin[x] = +/-1, Cos[y] = 1, Sin[y] = 0
						// => m[0][0] = Cos[z] and m[1][0] = Sin[z]
						rz = radToDeg((float)System.Math.Atan2(-mat[3], mat[0]));
					}
					// Standard case
					else
					{
						ry = radToDeg((float)System.Math.Atan2(mat[6], mat[8]));
						rz = radToDeg((float)System.Math.Atan2(mat[1], mat[4]));
					}
					return true;
				}

				rx = ry = rz = 0;
				return false;
			}

			public static void quaternionToRotation(float x, float y, float z, float w, out float rx, out float ry, out float rz)
			{
				var mat = new float[9];
				// Calculate coefficients
				float x2 = x + x, y2 = y + y, z2 = z + z;
				float xx = x * x2, xy = x * y2, xz = x * z2;
				float yy = y * y2, yz = y * z2, zz = z * z2;
				float wx = w * x2, wy = w * y2, wz = w * z2;

				mat[0] = 1 - (yy + zz);  mat[3] = xy - wz;          mat[6] = xz + wy;
				mat[1] = xy + wz;        mat[4] = 1 - (xx + zz);    mat[7] = yz - wx;
				mat[2] = xz - wy;        mat[5] = yz + wx;          mat[8] = 1 - (xx + yy);

				rotMatToRotation(mat, out rx, out ry, out rz);
			}
		};

		/**
		* \brief Convert to a uniform string only containing lower case letters and no white spaces
		*
		* @param str the string to convert
		* @return the converted string without any instances of ' ', '_', '-', or '|' and all upper case letters converted to lower case
		*/
		public static string removeWhiteSpacesAndToLower(string str)
		{
			string ret = str.ToLower();
			ret = ret.Replace(" ", "");
			ret = ret.Replace("_", "");
			ret = ret.Replace("|", "");
			return ret;
		}


		/**
		* \brief Get a string representing the joint name for a specific joint id
		*/
		public static string getJointName(SkeletonJoint id)
		{
			switch(id)
			{
			case SkeletonJoint.HEAD:
				return "head";
			case SkeletonJoint.NECK:
				return "neck";
			case SkeletonJoint.TORSO:
				return "torso";
			case SkeletonJoint.WAIST:
				return "waist";

			case SkeletonJoint.LEFT_SHOULDER:
				return "leftShoulder";
			case SkeletonJoint.LEFT_ELBOW:
				return "leftElbow";
			case SkeletonJoint.LEFT_WRIST:
				return "leftWrist";
			case SkeletonJoint.LEFT_HAND:
				return "leftHand";

			case SkeletonJoint.RIGHT_SHOULDER:
				return "rightShoulder";
			case SkeletonJoint.RIGHT_ELBOW:
				return "rightElbow";
			case SkeletonJoint.RIGHT_WRIST:
				return "rightWrist";
			case SkeletonJoint.RIGHT_HAND:
				return "rightHand";

			case SkeletonJoint.LEFT_HIP:
				return "leftHip";
			case SkeletonJoint.LEFT_KNEE:
				return "leftKnee";
			case SkeletonJoint.LEFT_ANKLE:
				return "leftAnkle";
			case SkeletonJoint.LEFT_FOOT:
				return "leftFoot";

			case SkeletonJoint.RIGHT_HIP:
				return "rightHip";
			case SkeletonJoint.RIGHT_KNEE:
				return "rightKnee";
			case SkeletonJoint.RIGHT_ANKLE:
				return "rightAnkle";
			case SkeletonJoint.RIGHT_FOOT:
				return "rightFoot";

			case SkeletonJoint.FACE_CHIN:
				return "faceChin";
			case SkeletonJoint.FACE_FOREHEAD:
				return "faceForeHead";
			case SkeletonJoint.FACE_LEFT_EAR:
				return "faceLeftEar";
			case SkeletonJoint.FACE_NOSE:
				return "faceNose";
			case SkeletonJoint.FACE_RIGHT_EAR:
				return "faceRightEar";
			default:
				return "";
			}
		}

		/**
		* \brief Get the joint id out of the given joint name as a string
		*/
		public static SkeletonJoint getJointID(string name)
		{
			if (name != null)
			{
				string lowerName = removeWhiteSpacesAndToLower(name);
				if (lowerName == "head")
					return SkeletonJoint.HEAD;
				if (lowerName == "neck")
					return SkeletonJoint.NECK;
				if (lowerName == "torso")			
					return SkeletonJoint.TORSO;
				if (lowerName == "waist")	
					return SkeletonJoint.WAIST;

				if (lowerName == "leftshoulder")
					return SkeletonJoint.LEFT_SHOULDER;
				if (lowerName == "leftelbow")
					return SkeletonJoint.LEFT_ELBOW;
				if (lowerName == "leftwrist")
					return SkeletonJoint.LEFT_WRIST;
				if (lowerName == "lefthand")
					return SkeletonJoint.LEFT_HAND;


				if (lowerName == "rightshoulder")			
					return SkeletonJoint.RIGHT_SHOULDER;
				if (lowerName == "rightelbow")
					return SkeletonJoint.RIGHT_ELBOW;
				if (lowerName == "rightwrist")
					return SkeletonJoint.RIGHT_WRIST;
				if (lowerName == "righthand")
					return SkeletonJoint.RIGHT_HAND;

				if (lowerName == "lefthip")
					return SkeletonJoint.LEFT_HIP;
				if (lowerName == "leftknee")
					return SkeletonJoint.LEFT_KNEE;
				if (lowerName == "leftankle")
					return SkeletonJoint.LEFT_ANKLE;
				if (lowerName == "leftfoot")
					return SkeletonJoint.LEFT_FOOT;

				if (lowerName == "righthip")
					return SkeletonJoint.RIGHT_HIP;
				if (lowerName == "rightknee")
					return SkeletonJoint.RIGHT_KNEE;
				if (lowerName == "rightankle")
					return SkeletonJoint.RIGHT_ANKLE;
				if (lowerName == "rightfoot")
					return SkeletonJoint.RIGHT_FOOT;

				if (lowerName == "facechin")
					return SkeletonJoint.FACE_CHIN;
				if (lowerName == "faceforehead")
					return SkeletonJoint.FACE_FOREHEAD;
				if (lowerName == "faceleftear")
					return SkeletonJoint.FACE_LEFT_EAR;
				if (lowerName == "facenose")
					return SkeletonJoint.FACE_NOSE;
				if (lowerName == "facerightear")
					return SkeletonJoint.FACE_RIGHT_EAR;
			}

			return SkeletonJoint.NUM_JOINTS;
		}

		/**
		* \brief Get the body measurement id out of the given measure name
		*/
		public static BodyMeasurement getBodyMeasureID(string name)
		{
			if (name != null)
			{
				string lowerName = removeWhiteSpacesAndToLower(name);
				if (lowerName =="bodyheight")
					return BodyMeasurement.BODY_HEIGHT;
				if (lowerName =="torsoheight")
					return BodyMeasurement.TORSO_HEIGHT;
				if (lowerName =="shoulderwidth")
					return BodyMeasurement.SHOULDER_WIDTH;
				if (lowerName =="hipwidth")
					return BodyMeasurement.HIP_WIDTH;
				if (lowerName =="armlength")
					return BodyMeasurement.ARM_LENGTH;
				if (lowerName =="upperarmlength")
					return BodyMeasurement.UPPER_ARM_LENGTH;
				if (lowerName =="lowerarmlength")
					return BodyMeasurement.LOWER_ARM_LENGTH;
				if (lowerName =="leglength")
					return BodyMeasurement.LEG_LENGTH;
				if (lowerName =="upperleglength")
					return BodyMeasurement.UPPER_LEG_LENGTH;
				if (lowerName =="lowerleglength")
					return BodyMeasurement.LOWER_LEG_LENGTH;
			}

			return BodyMeasurement.NUM_MEASUREMENTS;
		}

		/**
		* \brief Get the name string representation of a body measurement id
		*/
		public static string getBodyMeasureName(BodyMeasurement id)
		{
			switch(id)
			{
			case BodyMeasurement.BODY_HEIGHT:
				return "bodyHeight";
			case BodyMeasurement.TORSO_HEIGHT:
				return "torsoHeight";
			case BodyMeasurement.SHOULDER_WIDTH:
				return "shoulderWidth";
			case BodyMeasurement.HIP_WIDTH:
				return "hipWidth";
			case BodyMeasurement.ARM_LENGTH:
				return "armLength";
			case BodyMeasurement.UPPER_ARM_LENGTH:
				return "upperArmLength";
			case BodyMeasurement.LOWER_ARM_LENGTH:
				return "lowerArmLength";
			case BodyMeasurement.LEG_LENGTH:
				return "legLength";
			case BodyMeasurement.UPPER_LEG_LENGTH:
				return "upperLegLength";
			case BodyMeasurement.LOWER_LEG_LENGTH:
				return "lowerLegLength";
			default:
				return "";
			}
		}

		/**
		* \brief Get a string representing the hand joint name for a specific hand joint id
		*/
		public static string getHandJointName(SkeletonHandJoint id)
		{
			switch(id)
			{
			case SkeletonHandJoint.PALM:
				return "palm";
			case SkeletonHandJoint.THUMB:
				return "thumb";
			case SkeletonHandJoint.INDEX:
				return "index";
			case SkeletonHandJoint.MIDDLE:
				return "middle";
			case SkeletonHandJoint.RING:
				return "ring";
			case SkeletonHandJoint.PINKY:
				return "pinky";
			default:
				return "";
			}
		}

		/**
		* \brief Get the hand joint id out of the given hand joint name as a string
		*/
		public static SkeletonHandJoint getHandJointID(string name)
		{
			if (name != null)
			{
				string lowerName = removeWhiteSpacesAndToLower(name);
				if (lowerName == "palm")
					return SkeletonHandJoint.PALM;
				if (lowerName == "thumb")
					return SkeletonHandJoint.THUMB;
				if (lowerName == "index")			
					return SkeletonHandJoint.INDEX;
				if (lowerName == "middle")	
					return SkeletonHandJoint.MIDDLE;
				if (lowerName == "ring")
					return SkeletonHandJoint.RING;
				if (lowerName == "pinky")
					return SkeletonHandJoint.PINKY;
			}

			return SkeletonHandJoint.NUM_JOINTS;
		}

		public static string createCorrectionHintMsg(RecognitionCorrectionHint hint)
		{
			string msg = "";
			if (hint.m_failedState > -1)
			{
				msg += "State " + hint.m_failedState + " - ";
			}
			if (hint.m_changeType == RecognitionCorrectionHint.ChangeType.FINGERS)
			{
				msg += "Please show " + hint.m_dist.ToString("0") + ((hint.m_dist > 0) ? " more fingers!\n" : " less fingers!\n");
			}
			else if (hint.m_changeType == RecognitionCorrectionHint.ChangeType.FORM)
			{
				msg += "Please perform more accurately! Current distance to template:  " + hint.m_dist.ToString("0.###") + "\n";
			}
			else if (hint.m_changeType == RecognitionCorrectionHint.ChangeType.DIRECTION)
			{
				string action = hint.m_isAngle ? "turn" : "move";
				string direction = "";
				if (hint.m_dirX > 0.000001f)
					direction += hint.m_isAngle ? "up " : "right ";
				else if (hint.m_dirX < -0.000001f)
					direction += hint.m_isAngle ? "down " : "left ";
				if (hint.m_dirY > 0.000001f)
					direction += hint.m_isAngle ? "left " : "up ";
				else if (hint.m_dirY < -0.000001f)
					direction += hint.m_isAngle ? "right " : "down ";
				if (hint.m_dirZ > 0.000001f)
					direction += hint.m_isAngle ? "roll left " : "backward ";
				else if (hint.m_dirZ < -0.000001f)
					direction += hint.m_isAngle ? "roll right " : "forward ";
				msg += "Please " + action + " " + getJointName(hint.m_joint) + " "
					+ direction + ":" + hint.m_dirX.ToString("0.##") + "/" + hint.m_dirY.ToString("0.##") + "/" + hint.m_dirZ.ToString("0.##") + "\n";
			}
			else // SPEED or POSE
			{
				for (int dirI = 0; dirI < 3; ++dirI)
				{
					float value = hint.m_dirX;
					string direction = (value < 0) ? "left" : "right";
					if (hint.m_isAngle)
						direction = (value < 0) ? "down" : "up"; 
					if (dirI == 1)
					{
						value = hint.m_dirY;
						if (hint.m_isAngle)
							direction = (value < 0) ? "left" : "right"; 
						else
							direction = value < 0 ? "down" : "up";
					}
					else if (dirI == 2)
					{
						value = hint.m_dirZ;
						if (hint.m_isAngle)
							direction = (value < 0) ? "roll right" : "roll left";
						else
							direction = value < 0 ? "forward" : "backward";
					}
					if (System.Math.Abs(value) > 0.000001f)
					{
						string mod = "";
						string measure = "";
						if (hint.m_changeType == RecognitionCorrectionHint.ChangeType.POSE)
						{
							if (hint.m_changeDirection == RecognitionCorrectionHint.ChangeDirection.MORE)
								mod = "more ";
							else if (hint.m_changeDirection == RecognitionCorrectionHint.ChangeDirection.LESS)
								mod = "less ";
							if (hint.m_measuringUnit == BodyMeasurement.NUM_MEASUREMENTS)
								measure = hint.m_isAngle ? "°" : "mm";
							else
								measure = getBodyMeasureName(hint.m_measuringUnit);
						}
						else if (hint.m_changeType == RecognitionCorrectionHint.ChangeType.SPEED)
						{
							if (hint.m_changeDirection == RecognitionCorrectionHint.ChangeDirection.MORE)
								mod = "faster ";
							else if (hint.m_changeDirection == RecognitionCorrectionHint.ChangeDirection.LESS)
								mod = "slower ";
							measure = hint.m_isAngle ? "°/s" : "mm/s";
						}
						string action = hint.m_isAngle ? "turn" : "move";
						msg += "Please " + action + " " + getJointName(hint.m_joint) + " "
							+ mod + direction + ": " + value.ToString("0.##") + " " + measure + "\n";
					}
				}
				if (System.Math.Abs(hint.m_dist) > 0.000001f)
				{
					string direction = (hint.m_dist < 0) ? "closer" : "further";
					string measure = hint.m_isAngle ? "°" : "mm";
					if (hint.m_measuringUnit != BodyMeasurement.NUM_MEASUREMENTS)
						measure = getBodyMeasureName(hint.m_measuringUnit);
					msg += "Please move " + getJointName(hint.m_joint) + " "
						+ direction + ": " + hint.m_dist.ToString("0.##") + " " + measure + "\n";
				}
			}

			return msg;
		}
	}
}
