// ****************************************************************************************
//
// Fubi CS Wrapper
// ---------------------------------------------------------
// Copyright (C) 2010-2015 Felix Kistler 
// 
// This software is distributed under the terms of the Eclipse Public License v1.0.
// A copy of the license may be obtained at: http://www.eclipse.org/org/documents/epl-v10.html
// 
// ****************************************************************************************

using System;
using System.Runtime.InteropServices;

/** \file Fubi.cs 
 * \brief Contains the Fubi CS wrapper
*/

/**
 * \namespace FubiNET
 *
 * \brief The FubiNET namespace holds the Fubi class that wraps the C++ functions of Fubi
 *
 */
namespace FubiNET
{
	/**
	 *
	 * \brief The Fubi class wraps the C++ functions of Fubi.
	 *
	 */
	public class Fubi
	{
		/** \addtogroup FUBICSHARP FUBI C# API
		 * All the C# API functions (subset of the C++ functions + some additional ones)
		 * 
		 * @{
		 */

		/**
		* \brief Initializes Fubi with OpenN 1.x using the given xml file and sets the skeleton profile.
		*        If no xml file is given, Fubi will be intialized without OpenNI tracking enabled --> methods that need an openni context won't work.
		* 
		* @param openniXmlconfig name of the xml file for OpenNI 1.x initialization inlcuding all needed productions nodes 
		(should be placed in the working directory, i.e. "bin" folder)
		if config == 0x0, then OpenNI won't be initialized and Fubi stays in non-tracking mode
		* @param profile set the openNI skeleton profile
		* @param filterMinCutOffFrequency, filterVelocityCutOffFrequency, filterCutOffSlope options for filtering the tracking data if wanted
		* @param mirrorStreams whether the stream should be mirrored or not
		* @param registerStreams whether the depth stream should be registered to the color stream
		* @return true if succesfully initialized or already initialized before,
		false means bad xml file or other serious problem with OpenNI initialization
		*/
		public static bool init(string openniXmlconfig, FubiUtils.SkeletonProfile profile = FubiUtils.SkeletonProfile.ALL,
			float filterMinCutOffFrequency = 1.0f, float filterVelocityCutOffFrequency = 1.0f, float filterCutOffSlope = 0.007f,
			bool mirrorStreams = true, bool registerStreams = true)
		{
			var ret = true;
			if (!isInitialized())
			{
				var openNiXmlPtr = Marshal.StringToHGlobalAnsi(openniXmlconfig);
				ret = FubiInternal.init(openNiXmlPtr, profile, filterMinCutOffFrequency, filterVelocityCutOffFrequency, filterCutOffSlope, mirrorStreams, registerStreams);
				Marshal.FreeHGlobal(openNiXmlPtr);
			}
			return ret;
		}

		/**
		* \brief Initializes Fubi with an options file for the sensor init
		* 
		* @param sensorOptions configuration for the sensor
		* @param filterOptions filter options for additionally filtering of the tracking data
		* @return true if succesfully initialized or already initialized before,
		false means problem with sensor init
		*/
		public static bool init(FubiUtils.SensorOptions sensorOptions, FubiUtils.FilterOptions filterOptions)
		{
			var ret = true;
			if (!isInitialized())
			{
				ret = FubiInternal.init(sensorOptions.DepthOptions.Width, sensorOptions.DepthOptions.Height, sensorOptions.DepthOptions.Fps,
					sensorOptions.RGBOptions.Width, sensorOptions.RGBOptions.Height, sensorOptions.RGBOptions.Fps,
					sensorOptions.IROptions.Width, sensorOptions.IROptions.Height, sensorOptions.IROptions.Fps,
					sensorOptions.Type,
					sensorOptions.TrackingProfile,
					filterOptions.FilterMinCutOffFrequency, filterOptions.FilterVelocityCutOffFrequency, filterOptions.FilterCutOffSlope,
					sensorOptions.MirrorStreams, sensorOptions.RegisterStreams);
			}
			return ret;
		}

		/**
		* \brief Allows you to switch between different sensor types during runtime
		*		  Note that this will also reinitialize most parts of Fubi
		* 
		* @param options options for initializing the new sensor
		* @return true if the sensor has been succesfully initialized
		*/
		public static bool switchSensor(FubiUtils.SensorOptions options)
		{
			return FubiInternal.switchSensor(options.Type, options.DepthOptions.Width, options.DepthOptions.Height, options.DepthOptions.Fps,
					options.RGBOptions.Width, options.RGBOptions.Height, options.RGBOptions.Fps,
					options.IROptions.Width, options.IROptions.Height, options.IROptions.Fps,
					options.TrackingProfile,
					options.MirrorStreams, options.RegisterStreams);
		}

		/**
		* \brief Get the currently available sensor types (defined in FubiConfig.h before compilation)
		* 
		* @return an int composed of the currently available sensor types (see SensorType enum for the meaning)
		*/
		public static int getAvailableSensors()
		{
			return FubiInternal.getAvailableSensorTypes();
		}

		/**
		* \brief Get the type of the currently active sensor
		* 
		* @return the current sensor type
		*/
		public static FubiUtils.SensorType getCurrentSensorType()
		{
			return FubiInternal.getCurrentSensorType();
		}

		/**
		* \brief Shuts down Fubi and its sensors releasing all allocated memory
		* 
		*/
		public static void release()
		{
			FubiInternal.release();
		}

		/**
		* \brief Updates the current sensor to get the next frame of depth, rgb/ir, and tracking data.
		*        Also searches for users in the scene to start tracking them
		* 
		*/
		public static void updateSensor()
		{
			FubiInternal.updateSensor();
		}

		/**
		* \brief retrieve an image from one of the sensor streams with specific format and optionally enhanced by different
		*        tracking information 
		*		  Some render options require an OpenCV installation!
		*
		* @param[out] outputImage pointer to a unsigned char array
		*        Will be filled with wanted image
		*		  Array has to be of correct size, e.g. depth image (640x480 std resolution) with tracking info
		*		  requires 4 channels (RGBA) --> size = 640*480*4 = 1228800
		* @param type can be color, depth, ir image, or blank for only rendering tracking info
		* @param numChannels number channels in the image 1, 3 or 4
		* @param depth the pixel depth of the image, 8 bit (standard), 16 bit (mainly usefull for depth images) or 32 bit float
		* @param renderOptions options for rendering additional informations into the image (e.g. tracking skeleton) or swapping the r and b channel
		* @param jointsToRender defines for which of the joints the trackinginfo (see renderOptions) should be rendererd
		* @param depthModifications options for transforming the depth image to a more visible format
		* @param userID If set to something else than 0 an image will be cut cropped around (the joint of interest of) this user, if 0 the whole image is put out.
		* @param jointOfInterest the joint of the user the image is cropped around and a threshold on the depth values is applied.
		If set to num_joints fubi tries to crop out the whole user.
		* @param moveCroppedToUpperLeft moves the cropped image to the upper left corner
		*/
		public static void getImage(byte[] outputImage, FubiUtils.ImageType type, FubiUtils.ImageNumChannels numChannels, FubiUtils.ImageDepth depth,
			int renderOptions = (int)FubiUtils.RenderOptions.Default,
			int jointsToRender = (int)FubiUtils.JointsToRender.ALL_JOINTS,
			FubiUtils.DepthImageModification depthModifications = FubiUtils.DepthImageModification.UseHistogram,
			uint userID = 0, FubiUtils.SkeletonJoint jointOfInterest = FubiUtils.SkeletonJoint.NUM_JOINTS, bool moveCroppedToUpperLeft = false)
		{
			if (outputImage != null)
			{
				var h = GCHandle.Alloc(outputImage, GCHandleType.Pinned);
				FubiInternal.getImage(h.AddrOfPinnedObject(), type, numChannels, depth, renderOptions, jointsToRender, depthModifications, userID, jointOfInterest, moveCroppedToUpperLeft);
				h.Free();
			}
		}

		/**
		* \brief Tries to recognize a posture in the current frame of tracking data of one user
		* 
		* @param postureID enum id of the posture to be found in FubiPredefinedGestures.h
		* @param userID the id of the user to be checked
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult recognizeGestureOn(FubiPredefinedGestures.Postures postureID, UInt32 userID)
		{
			return FubiInternal.recognizeGestureOn(postureID, userID);
		}

		/**
		* \brief Checks a user defined gesture or posture recognizer for its success
		* 
		* @param recognizerIndex id of the recognizer return during its creation
		* @param userID the id of the user to be checked
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult recognizeGestureOn(UInt32 recognizerIndex, UInt32 userID)
		{
			return FubiInternal.recognizeGestureOn(recognizerIndex, userID);
		}
		/**
		* \brief Checks a user defined gesture or posture recognizer for its success
		* 
		* @param recognizerIndex id of the recognizer return during its creation
		* @param userID the id of the user to be checked
		* @param[out] correctionHint will contain information about why the recognition failed if wanted
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult recognizeGestureOn(UInt32 recognizerIndex, UInt32 userID, out FubiUtils.RecognitionCorrectionHint correctionHint)
		{
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var res = FubiInternal.recognizeGestureOn(recognizerIndex, userID, hint);
			correctionHint = hint;
			return res;
		}

		/**
		* \brief Returns user id from the user index
		* 
		* @param index index of the user in the user array
		* @return id of that user or 0 if not found
		*/
		public static UInt32 getUserID(UInt32 index)
		{
			return FubiInternal.getUserID(index);
		}

		/**
		* \brief Automatically starts combination recognition for new users
		* 
		* @param enable if set to true, the recognizer will automatically start for new users, else this must be done manually (by using enableCombinationRecognition(..))
		* @param combinationID enum id of the combination to be found in FubiPredefinedGestures.h or Combinations::NUM_COMBINATIONS for all combinations (also the user defined ones!)
		*/
		public static void setAutoStartCombinationRecognition(bool enable, FubiPredefinedGestures.Combinations combinationID = FubiPredefinedGestures.Combinations.NUM_COMBINATIONS)
		{
			FubiInternal.setAutoStartCombinationRecognition(enable, combinationID);
		}

		/**
		* \brief Check if autostart is activated for a combination recognizer
		* 
		* @param combinationID enum id of the combination to be found in FubiPredefinedGestures.h orCombinations::NUM_COMBINATIONS for all combinations
		* @return true if the corresponding auto start is activated
		*/
		public static bool getAutoStartCombinationRecognition(FubiPredefinedGestures.Combinations combinationID = FubiPredefinedGestures.Combinations.NUM_COMBINATIONS)
		{
			return FubiInternal.getAutoStartCombinationRecognition(combinationID);
		}

		/**
		* \brief Starts or stops the recognition process of a combination for one user
		* 
		* @param combinationID enum id of the combination to be found in FubiPredefinedGestures.h or Combinations::NUM_COMBINATIONS for all combinations
		* @param userID the id of the user for whom the recognizers should be modified
		* @param enable if set to true, the recognizer will be started (if not already stared), else it stops
		*/
		public static void enableCombinationRecognition(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, bool enable)
		{
			FubiInternal.enableCombinationRecognition(combinationID, userID, enable);
		}

		/**
		* \brief Checks a combination recognizer for its progress
		* 
		* @param combinationID  enum id of the combination to be found in FubiPredefinedGestures.h
		* @param userID the id of the user to be checked
		* @param[out] correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, out FubiUtils.RecognitionCorrectionHint correctionHint, bool restart = true)
		{
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var res = FubiInternal.getCombinationRecognitionProgressOn(combinationID, userID, new IntPtr(0), restart, false, hint);
			correctionHint = hint;
			return res;
		}

		/**
		* \brief Checks a combination recognizer for its progress
		* 
		* @param combinationID  enum id of the combination to be found in FubiPredefinedGestures.h
		* @param userID the id of the user to be checked
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, bool restart = true)
		{
			return FubiInternal.getCombinationRecognitionProgressOn(combinationID, userID, new IntPtr(0), restart);
		}

		/**
		* \brief Checks a combination recognizer for its progress
		* 
		* @param combinationID  enum id of the combination to be found in FubiPredefinedGestures.h
		* @param userID the id of the user to be checked
		* @param userStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		*		  during the recognition of each state
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @param returnFilteredData if true, the user states vector will contain filtered data
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, out FubiTrackingData[] userStates,
			bool restart = true, bool returnFilteredData = false)
		{
			FubiUtils.RecognitionCorrectionHint dummy;
			return getCombinationRecognitionProgressOn(combinationID, userID, out userStates, out dummy, restart, returnFilteredData);
		}

		/**
		* \brief Checks a combination recognizer for its progress
		* 
		* @param combinationID  enum id of the combination to be found in FubiPredefinedGestures.h
		* @param userID the id of the user to be checked
		* @param userStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		*		  during the recognition of each state
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @param returnFilteredData if true, the user states vector will contain filtered data
		* @param[out] correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, out FubiTrackingData[] userStates,
			out FubiUtils.RecognitionCorrectionHint correctionHint, bool restart = true, bool returnFilteredData = false)
		{
			var vec = FubiInternal.createTrackingDataVector();
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var recognized = FubiInternal.getCombinationRecognitionProgressOn(combinationID, userID, vec, restart, returnFilteredData, hint);
			correctionHint = hint;
			if (recognized == FubiUtils.RecognitionResult.RECOGNIZED)
			{
				var size = FubiInternal.getTrackingDataVectorSize(vec);
				userStates = new FubiTrackingData[size];
				for (UInt32 i = 0; i < size; i++)
				{
					var tInfo = FubiInternal.getTrackingData(vec, i);
					var info = new FubiTrackingData();
					for (UInt32 j = 0; j < (uint)FubiUtils.SkeletonJoint.NUM_JOINTS; ++j)
					{
						FubiInternal.getSkeletonJointPosition(tInfo, (FubiUtils.SkeletonJoint)j, out info.JointPositions[j].X, out info.JointPositions[j].Y, out info.JointPositions[j].Z, out info.JointPositions[j].Confidence, out info.TimeStamp);
						double timeStamp;
						var rotMat = new float[9];
						FubiInternal.getSkeletonJointOrientation(tInfo, (FubiUtils.SkeletonJoint)j, rotMat, out info.JointPositions[j].Confidence, out timeStamp);
						FubiUtils.Math.rotMatToRotation(rotMat, out info.JointOrientations[j].Rx, out info.JointOrientations[j].Ry, out info.JointOrientations[j].Rz);
						info.TimeStamp = Math.Max(timeStamp, info.TimeStamp);
					}
					userStates[i] = info;
				}
				FubiInternal.releaseTrackingDataVector(vec);
			}
			else
				userStates = null;
			return recognized;
		}

		/**
		* \brief Loads a recognizer config xml file and adds the configured recognizers
		* 
		* @param fileName name of the xml config file
		* @return true if at least one recognizers was loaded from the given xml
		*/
		public static bool loadRecognizersFromXML(string fileName)
		{
			var fileNamePtr = Marshal.StringToHGlobalAnsi(fileName);
			var ret = FubiInternal.loadRecognizersFromXML(fileNamePtr);
			Marshal.FreeHGlobal(fileNamePtr);
			return ret;
		}

		/**
		* \brief Checks a user defined gesture or posture recognizer for its success
		* 
		* @param recognizerName name of the recognizer return during its creation
		* @param userID the id of the user to be checked
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult recognizeGestureOn(string recognizerName, UInt32 userID)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.recognizeGestureOn(namePtr, userID);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Checks a user defined gesture or posture recognizer for its success
		* 
		* @param recognizerName name of the recognizer return during its creation
		* @param userID the id of the user to be checked
		* @param[out] correctionHint will contain information about why the recognition failed if wanted
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult recognizeGestureOn(string recognizerName, UInt32 userID, out FubiUtils.RecognitionCorrectionHint correctionHint)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var ret = FubiInternal.recognizeGestureOn(namePtr, userID, hint);
			correctionHint = hint;
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Returns true if Fubi has been already initialized
		* 
		*/
		public static bool isInitialized()
		{
			return FubiInternal.isInitialized();
		}

		/**
		* \brief Returns current number of user defined recognizers
		* 
		* @return number of recognizers, the recognizers also have the indices 0 to numberOfRecs-1
		*/
		public static UInt32 getNumUserDefinedRecognizers()
		{
			return FubiInternal.getNumUserDefinedRecognizers();
		}

		/**
		* \brief Returns the current depth resolution or -1, -1 if failed
		* 
		* @param[out] width, height the resolution
		*/
		public static void getDepthResolution(out Int32 width, out Int32 height)
		{
			FubiInternal.getDepthResolution(out width, out height);
		}

		/**
		* \brief Returns the current rgb resolution or -1, -1 if failed
		* 
		* @param[out] width, height the resolution
		*/
		public static void getRgbResolution(out Int32 width, out Int32 height)
		{
			FubiInternal.getRgbResolution(out width, out height);
		}

		/**
		* \brief Returns the current ir resolution or -1, -1 if failed
		* 
		* @param[out] width, height the resolution
		*/
		public static void getIRResolution(out Int32 width, out Int32 height)
		{
			FubiInternal.getIRResolution(out width, out height);
		}

		/**
		* \brief Returns the name of a user defined recognizer
		* 
		* @param  recognizerIndex index of the recognizer
		* @return returns the recognizer name or an empty string if the user is not found or the name not set
		*/
		public static string getUserDefinedRecognizerName(UInt32 recognizerIndex)
		{
			var namePtr = FubiInternal.getUserDefinedRecognizerName(recognizerIndex);
			return Marshal.PtrToStringAnsi(namePtr);
		}

		/**
		* \brief Returns the index of a user defined recognizer
		* 
		* @param recognizerName name of the recognizer
		* @return returns the recognizer name or -1 if not found
		*/
		public static Int32 getUserDefinedRecognizerIndex(string recognizerName)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.getUserDefinedRecognizerIndex(namePtr);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Returns the number of shown fingers detected at the hand of one user (REQUIRES OPENCV!)
		* 
		* @param userID FUBI id of the user
		* @param leftHand looks at the left instead of the right hand
		* @param getMedianOfLastFrames uses the precalculated median of finger counts of the last frames (still calculates new one if there is no precalculation)
		* @param medianWindowSize defines the number of last frames that is considered for calculating the median
		* @return the number of shown fingers detected, 0 if there are none or there is an error
		*/
		public static Int32 getFingerCount(UInt32 userID, bool leftHand = false, bool getMedianOfLastFrames = true, UInt32 medianWindowSize = 10)
		{
			return FubiInternal.getFingerCount(userID, leftHand, getMedianOfLastFrames, medianWindowSize);
		}

		/**
		* \brief  Get the finger count of a hand
		*
		* @param handId
		* @return the number of shown fingers detected, 0 if there are none or there is an error
		*/
		public static Int32 getHandFingerCount(uint handId)
		{
			var info = FubiInternal.getCurrentFingerTrackingData(handId, false);
			if (info.ToInt32() != 0)
				return FubiInternal.getHandFingerCount(info);
			else
				return 0;
		}

		/**
		* \brief Checks a user defined combination recognizer for its progress
		* 
		* @param recognizerName name of the combination
		* @param userID the id of the user to be checked
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(string recognizerName, UInt32 userID,
			bool restart = true)
		{
			FubiUtils.RecognitionCorrectionHint hint;
			return getCombinationRecognitionProgressOn(recognizerName, userID, out hint, restart);
		}

		/**
		* \brief Checks a user defined combination recognizer for its progress
		* 
		* @param recognizerName name of the combination
		* @param userID the id of the user to be checked
		* @param[out] correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(string recognizerName, UInt32 userID,
			out FubiUtils.RecognitionCorrectionHint correctionHint, bool restart = true)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var ret = FubiInternal.getCombinationRecognitionProgressOn(namePtr, userID, new IntPtr(0), restart, false, hint);
			correctionHint = hint;
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Checks a user defined combination recognizer for its progress
		* 
		* @param recognizerName name of the combination
		* @param userID the id of the user to be checked
		* @param userStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		*		  during the recognition of each state
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @param returnFilteredData if true, the user states vector will contain filtered data
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(string recognizerName, UInt32 userID, out FubiTrackingData[] userStates,
		   bool restart = true, bool returnFilteredData = false)
		{
			FubiUtils.RecognitionCorrectionHint hint;
			return getCombinationRecognitionProgressOn(recognizerName, userID, out userStates, out hint, restart, returnFilteredData);
		}

		/**
		* \brief Checks a user defined combination recognizer for its progress
		* 
		* @param recognizerName name of the combination
		* @param userID the id of the user to be checked
		* @param userStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		*		  during the recognition of each state
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @param returnFilteredData if true, the user states vector will contain filtered data
		* @param[out] correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(string recognizerName, UInt32 userID, out FubiTrackingData[] userStates,
			out FubiUtils.RecognitionCorrectionHint correctionHint, bool restart = true, bool returnFilteredData = false)
		{
			var vec = FubiInternal.createTrackingDataVector();
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var recognized = FubiInternal.getCombinationRecognitionProgressOn(namePtr, userID, vec, restart, returnFilteredData, hint);
			correctionHint = hint;
			Marshal.FreeHGlobal(namePtr);
			if (recognized == FubiUtils.RecognitionResult.RECOGNIZED)
			{
				var size = FubiInternal.getTrackingDataVectorSize(vec);
				userStates = new FubiTrackingData[size];
				for (UInt32 i = 0; i < size; i++)
				{
					var tInfo = FubiInternal.getTrackingData(vec, i);
					var info = new FubiTrackingData();
					for (UInt32 j = 0; j < (uint)FubiUtils.SkeletonJoint.NUM_JOINTS; ++j)
					{
						FubiInternal.getSkeletonJointPosition(tInfo, (FubiUtils.SkeletonJoint)j, out info.JointPositions[j].X, out info.JointPositions[j].Y, out info.JointPositions[j].Z, out info.JointPositions[j].Confidence, out info.TimeStamp);
						double timeStamp;
						var rotMat = new float[9];
						FubiInternal.getSkeletonJointOrientation(tInfo, (FubiUtils.SkeletonJoint)j, rotMat, out info.JointPositions[j].Confidence, out timeStamp);
						FubiUtils.Math.rotMatToRotation(rotMat, out info.JointOrientations[j].Rx, out info.JointOrientations[j].Ry, out info.JointOrientations[j].Rz);
						info.TimeStamp = Math.Max(timeStamp, info.TimeStamp);
					}
					userStates[i] = info;
				}
			}
			else
				userStates = null;

			FubiInternal.releaseTrackingDataVector(vec);
			return recognized;
		}

		/**
		* \brief Returns the index of a user defined combination recognizer
		* 
		* @param recognizerName name of the recognizer
		* @return returns the recognizer name or -1 if not found
		*/
		public static Int32 getUserDefinedCombinationRecognizerIndex(string recognizerName)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.getUserDefinedCombinationRecognizerIndex(namePtr);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Returns current number of user defined combination recognizers
		* 
		* @return number of recognizers, the recognizers also have the indices 0 to numberOfRecs-1
		*/
		public static UInt32 getNumUserDefinedCombinationRecognizers()
		{
			return FubiInternal.getNumUserDefinedCombinationRecognizers();
		}

		/**
		* \brief Returns the name of a user defined combination recognizer
		* 
		* @param  recognizerIndex index of the recognizer
		* @return returns the recognizer name or an empty string if the user is not found or the name not set
		*/
		public static string getUserDefinedCombinationRecognizerName(UInt32 recognizerIndex)
		{
			return Marshal.PtrToStringAnsi(FubiInternal.getUserDefinedCombinationRecognizerName(recognizerIndex));
		}

		/**
		* \brief Starts or stops the recognition process of a user defined combination for one user
		* 
		* @param combinationName name defined for this recognizer
		* @param userID the id of the user for whom the recognizers should be modified
		* @param enable if set to true, the recognizer will be started (if not already stared), else it stops
		*/
		public static void enableCombinationRecognition(string combinationName, UInt32 userID, bool enable)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(combinationName);
			FubiInternal.enableCombinationRecognition(namePtr, userID, enable);
			Marshal.FreeHGlobal(namePtr);
		}

		/**
		* \brief Returns the color for a user in the background image
		* 
		* @param id OpennNI user id of the user of interest
		* @param[out] r, g, b returns the red, green, and blue components of the color in which the users shape is displayed in the tracking image
		*/
		public static void getColorForUserID(UInt32 id, out float r, out float g, out float b)
		{
			FubiInternal.getColorForUserID(id, out r, out g, out b);
		}

		/**
		* \brief save an image from one of the sensor streams with specific format and optionally enhanced by different
		*        tracking information
		*
		* @param fileName filename where the image should be saved to
		*        can be relative to the working directory (bin folder) or absolute
		*		  the file extension determins the file format (should be jpg)
		* @param jpegQuality qualitiy (= 88) of the jpeg compression if a jpg file is requested, ranges from 0 to 100 (best quality)
		* @param type can be color, depth, or ir image
		* @param numChannels number channels in the image 1, 3 or 4
		* @param depth the pixel depth of the image, 8 bit (standard) or 16 bit (mainly usefull for depth images
		* @param renderOptions options for rendering additional informations into the image (e.g. tracking skeleton) or swapping the r and b channel
		* @param jointsToRender defines for which of the joints the trackinginfo (see renderOptions) should be rendererd
		* @param depthModifications options for transforming the depht image to a more visible format
		* @param userID If set to something else than 0 an image will be cut cropped around (the joint of interest of) this user, if 0 the whole image is put out.
		* @param jointOfInterest the joint of the user the image is cropped around and a threshold on the depth values is applied.
		If set to num_joints fubi tries to crop out the whole user.
		*/
		public static bool saveImage(string fileName, int jpegQuality /*0-100*/,
			FubiUtils.ImageType type, FubiUtils.ImageNumChannels numChannels, FubiUtils.ImageDepth depth,
			int renderOptions = (int)FubiUtils.RenderOptions.Default,
			int jointsToRender = (int)FubiUtils.JointsToRender.ALL_JOINTS,
			FubiUtils.DepthImageModification depthModifications = FubiUtils.DepthImageModification.UseHistogram,
			UInt32 userID = 0, FubiUtils.SkeletonJoint jointOfInterest = FubiUtils.SkeletonJoint.NUM_JOINTS)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(fileName);
			var ret = FubiInternal.saveImage(namePtr, jpegQuality, type, numChannels, depth, renderOptions, jointsToRender, depthModifications, userID, jointOfInterest);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Creates a user defined joint relation recognizer
		* 
		* @param joint the joint of interest
		* @param relJoint the joint in which it has to be in a specific relation
		* @param minX, minY, minZ (=-inf, -inf, -inf) the minimal values allowed for the vector relJoint -> joint
		* @param maxX, maxY, maxZ (=inf, inf, inf) the maximal values allowed for the vector relJoint -> joint
		* @param minDistance (= 0) the minimal distance between joint and relJoint
		* @param maxDistance (= inf) the maximal distance between joint and relJoint
		* @param midJoint (=num_joints)
		* @param midJointMinX, midJointMinY, midJointMinZ (=-inf, -inf, -inf) the minimal values allowed for the vector from the line segment (relJoint-joint) -> midJoint
		* @param midJointMaxX, midJointMaxY, midJointMaxZ (=inf, inf, inf) the maximal values allowed for the vector from the line segment (relJoint-joint) -> midJoint
		* @param midJointMinDistance (= 0) the minimal distance allowed from the line segment (relJoint-joint) -> midJoint
		* @param midJointMaxDistance (= inf) the maximal distance allowed from the line segment (relJoint-joint) -> midJoint
		* @param useLocalPositions use positions in the local coordinate system of the user based on the torso transformation
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name (= 0) sets a name for the recognizer (should be unique!)
		* @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		* @param measuringUnit the measuring unit for the values (millimeter by default)
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static UInt32 addJointRelationRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
			float minX = -FubiUtils.Math.MaxFloat, float minY = -FubiUtils.Math.MaxFloat, float minZ = -FubiUtils.Math.MaxFloat,
			float maxX = FubiUtils.Math.MaxFloat, float maxY = FubiUtils.Math.MaxFloat, float maxZ = FubiUtils.Math.MaxFloat,
			float minDistance = 0, float maxDistance = FubiUtils.Math.MaxFloat,
			FubiUtils.SkeletonJoint midJoint = FubiUtils.SkeletonJoint.NUM_JOINTS,
			float midJointMinX = -FubiUtils.Math.MaxFloat, float midJointMinY = -FubiUtils.Math.MaxFloat, float midJointMinZ = -FubiUtils.Math.MaxFloat,
			float midJointMaxX = FubiUtils.Math.MaxFloat, float midJointMaxY = FubiUtils.Math.MaxFloat, float midJointMaxZ = FubiUtils.Math.MaxFloat,
			float midJointMinDistance = 0, 
			float midJointMaxDistance = FubiUtils.Math.MaxFloat,
			bool useLocalPositions = false,
			Int32 atIndex = -1, string name = null,
			float minConfidence = -1.0f, FubiUtils.BodyMeasurement measuringUnit = FubiUtils.BodyMeasurement.NUM_MEASUREMENTS,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addJointRelationRecognizer(joint, relJoint, minX, minY, minZ, maxX, maxY, maxZ, minDistance, maxDistance,
				midJoint, midJointMinX, midJointMinY, midJointMinZ, midJointMaxX, midJointMaxY, midJointMaxZ, midJointMinDistance, midJointMaxDistance,
				useLocalPositions, atIndex, namePtr, minConfidence, measuringUnit, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Creates a user defined finger count recognizer
		* 
		* @param handJoint the hand joint of interest
		* @param minFingers the minimum number of fingers the user should show up
		* @param maxFingers the maximum number of fingers the user should show up
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name (= 0) sets a name for the recognizer (should be unique!)
		* @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		* @param useMedianCalculation (=false) if true, the median for the finger count will be calculated over several frames instead of always taking the current detection
		* @param medianWindowSize (=10) defines the window size for calculating the median (=number of frames)
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static UInt32 addFingerCountRecognizer(FubiUtils.SkeletonJoint handJoint,
			UInt32 minFingers, UInt32 maxFingers,
			Int32 atIndex = -1,
			string name = null,
			float minConfidence = -1.0f,
			bool useMedianCalculation = false,
			UInt32 medianWindowSize = 10,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addFingerCountRecognizer(handJoint, minFingers, maxFingers,
				atIndex, namePtr, minConfidence, useMedianCalculation, medianWindowSize, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Creates a user defined joint orientation recognizer
		* 
		* @param joint the joint of interest
		* @param minX, minY, minZ (=-180, -180, -180) the minimal degrees allowed for the joint orientation
		* @param maxX, maxY, maxZ (=180, 180, 180) the maximal degrees allowed for the joint orientation
		* @param useLocalOrientations if true, uses a local orienation in which the parent orientation has been substracted
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name (= 0) sets a name for the recognizer (should be unique!)
		* @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static UInt32 addJointOrientationRecognizer(FubiUtils.SkeletonJoint joint,
			float minX = -180.0f, float minY = -180.0f, float minZ = -180.0f,
			float maxX = 180.0f, float maxY = 180.0f, float maxZ = 180.0f,
			bool useLocalOrientations = true,
			int atIndex = -1,
			string name = null,
			float minConfidence = -1,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addJointOrientationRecognizer(joint, minX, minY, minZ, maxX, maxY, maxZ,
				useLocalOrientations, atIndex, namePtr, minConfidence, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Creates a user defined joint orientation recognizer
		* 
		* @param joint the joint of interest
		* @param orientX, orientY, orientZ indicate the wanted joint orientation
		* @param maxAngleDifference (=45°) the maximum angle difference that is allowed between the requested orientation and the actual orientation
		* @param useLocalOrientations if true, uses a local orienation in which the parent orientation has been substracted
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name (= 0) sets a name for the recognizer (should be unique!)
		* @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static UInt32 addJointOrientationRecognizer(FubiUtils.SkeletonJoint joint,
			float orientX, float orientY, float orientZ,
			float maxAngleDifference = 45.0f,
			bool useLocalOrientations = true,
			int atIndex = -1,
			string name = null,
			float minConfidence = -1,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addJointOrientationRecognizer(joint, orientX, orientY, orientZ, maxAngleDifference,
				useLocalOrientations, atIndex, namePtr, minConfidence, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}


		/**
		* \brief Creates a user defined linear movement recognizer
		* A linear gesture has a vector calculated as joint - relative joint, 
		* the direction (each component -1 to +1) that will be applied per component on the vector, and a min and max vel in milimeter per second
		* 
		* @param joint the joint of interest
		* @param relJoint the joint in which it has to be in a specifc relation
		* @param dirX, dirY, dirZ the direction in which the movement should happen
		* @param minVel the minimal velocity that has to be reached in this direction
		* @param maxVel (= inf) the maximal velocity that is allowed in this direction
		* @param useLocalPositions use positions in the local coordinate system of the user based on the torso transformation
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name name of the recognizer
		* @param maxAngleDifference (=45°) the maximum angle difference that is allowed between the requested direction and the actual movement direction
		* @param useOnlyCorrectDirectionComponent (=true) If true, this only takes the component of the actual movement that is conform
		*				the requested direction, else it always uses the actual movement for speed calculation
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
			float dirX, float dirY, float dirZ, float minVel = 0, float maxVel = FubiUtils.Math.MaxFloat,
			bool useLocalPositions = false,
			int atIndex = -1, string name = null, float maxAngleDifference = 45.0f, bool useOnlyCorrectDirectionComponent = true,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addLinearMovementRecognizer(joint, relJoint, dirX, dirY, dirZ, minVel, maxVel,
				useLocalPositions, atIndex, namePtr, maxAngleDifference, useOnlyCorrectDirectionComponent, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}
		public static UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint,
			float dirX, float dirY, float dirZ, float minVel = 0, float maxVel = FubiUtils.Math.MaxFloat,
			bool useLocalPositions = false,
			int atIndex = -1, string name = null, float maxAngleDifference = 45.0f, bool useOnlyCorrectDirectionComponent = true,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addLinearMovementRecognizer(joint, dirX, dirY, dirZ, minVel, maxVel,
				useLocalPositions, atIndex, namePtr, maxAngleDifference, useOnlyCorrectDirectionComponent, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Creates a user defined linear movement recognizer
		* A linear gesture has a vector calculated as joint - relative joint, 
		* the direction (each component -1 to +1) that will be applied per component on the vector, and a min and max vel in milimeter per second
		* 
		* @param joint the joint of interest
		* @param relJoint the joint in which it has to be in a specifc relation
		* @param dirX, dirY, dirZ the direction in which the movement should happen
		* @param minVel the minimal velocity that has to be reached in this direction
		* @param maxVel (= inf) the maximal velocity that is allowed in this direction
		* @param minLength the minimal length of path that has to be reached (only works within a combination rec)
		* @param maxLength the maximal length of path that can be reached (only works within a combination rec)
		* @param measuringUnit measuring unit for the path length
		* @param useLocalPositions use positions in the local coordinate system of the user based on the torso transformation
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name name of the recognizer
		* @param maxAngleDifference (=45°) the maximum angle difference that is allowed between the requested direction and the actual movement direction
		* @param useOnlyCorrectDirectionComponent (=true) If true, this only takes the component of the actual movement that is conform
		*				the requested direction, else it always uses the actual movement for speed calculation
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
			float dirX, float dirY, float dirZ, float minVel, float maxVel,
			float minLength, float maxLength, FubiUtils.BodyMeasurement measuringUnit,
			bool useLocalPositions = false,
			int atIndex = -1, string name = null, float maxAngleDifference = 45.0f, bool useOnlyCorrectDirectionComponent = true,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addLinearMovementRecognizer(joint, relJoint, dirX, dirY, dirZ, minVel, maxVel,
				minLength, maxLength, measuringUnit, useLocalPositions, atIndex, namePtr, maxAngleDifference,
				useOnlyCorrectDirectionComponent, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}
		public static UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint,
			float dirX, float dirY, float dirZ, float minVel, float maxVel,
			float minLength, float maxLength, FubiUtils.BodyMeasurement measuringUnit,
			bool useLocalPositions = false,
			int atIndex = -1, string name = null, float maxAngleDifference = 45.0f, bool useOnlyCorrectDirectionComponent = true,
			bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addLinearMovementRecognizer(joint, dirX, dirY, dirZ, minVel, maxVel,
				minLength, maxLength, measuringUnit, useLocalPositions, atIndex, namePtr, maxAngleDifference,
				useOnlyCorrectDirectionComponent, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief load a combination recognizer from a string that represents an xml node with the combination definition
		* 
		* @param xmlDefinition string containing the xml definition
		* @return true if the combination was loaded succesfully
		*/ 
		public static bool addCombinationRecognizer(string xmlDefinition)
		{
			var stringPtr = Marshal.StringToHGlobalAnsi(xmlDefinition);
			var ret = FubiInternal.addCombinationRecognizer(stringPtr);
			Marshal.FreeHGlobal(stringPtr);
			return ret;
		}

		/**
		* \brief  Whether the user is currently seen in the depth image
		*
		* @param userID FUBI id of the user
		*/
		public static bool isUserInScene(UInt32 userID)
		{
			return FubiInternal.isUserInScene(userID);
		}

		/**
		* \brief Whether the user is currently tracked
		*
		* @param userID FUBI id of the user
		*/
		public static bool isUserTracked(UInt32 userID)
		{
			return FubiInternal.isUserTracked(userID);
		}

		/**
		* \brief Get a joint position of a tracked user
		*
		* @param userID FUBI id of the user
		* @param joint the Joint to get the position from
		* @param[out] x, y, z the position for that joint
		* @param[out] confidence the tracking confidence
		* @param[out] timeStamp the time at which the tracking position was last updated
		* @param useLocalPositions (=false) whether to return the position in global or local coordinates
		* @param useFilteredData (=false) whether to return the positions with applied filtering
		*/
		public static void getCurrentSkeletonJointPosition(UInt32 userID, FubiUtils.SkeletonJoint joint,
			out float x, out float y, out float z, out float confidence, out double timeStamp,
			bool useLocalPositions = false, bool useFilteredData = false)
		{
			var info = FubiInternal.getCurrentTrackingData(userID, useFilteredData);
			FubiInternal.getSkeletonJointPosition(info, joint, out x, out y, out z, out confidence, out timeStamp, useLocalPositions);
		}

		/**
		* \brief Get a joint orientation of a tracked user
		*
		* @param userID FUBI id of the user
		* @param joint the Joint to get the orientation from
		* @param[out] mat the 3x3 orientation matrix for that joint
		* @param[out] confidence the tracking confidence
		* @param[out] timeStamp the time at which the tracking orientation was last updated
		* @param localOrientations (=true) if set to true, the function will local orientations (cleared of parent orientation) instead of globals
		* @param useFilteredData (=false) whether to return the orientation with applied filtering
		*/
		public static void getCurrentSkeletonJointOrientation(UInt32 userID, FubiUtils.SkeletonJoint joint,
			float[] mat, out float confidence, out double timeStamp,
			bool localOrientations = true, bool useFilteredData = false)
		{
			var info = FubiInternal.getCurrentTrackingData(userID, useFilteredData);
			if (info.ToInt32() != 0)
			{
				FubiInternal.getSkeletonJointOrientation(info, joint, mat, out confidence, out timeStamp, localOrientations);
			}
			else
			{
				float[] temp = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
				Array.Copy(temp, mat, temp.Length);
				confidence = 0;
				timeStamp = 0;
			}
		}

		/**
		* \brief  Get a users current value of a specific body measurement distance
		*
		* @param userId id of the user
		* @param measure the requested measurement
		* @param[out] dist the actual distance value
		* @param[out] confidence the confidence for this position
		*/
		public static void getBodyMeasurementDistance(uint userId, FubiUtils.BodyMeasurement measure, out float dist,
			out float confidence)
		{
			FubiInternal.getBodyMeasurementDistance(userId, measure, out dist, out confidence);
		}

		/**
		* \brief Stops and removes all user defined recognizers
		*/
		public static void clearUserDefinedRecognizers()
		{
			FubiInternal.clearUserDefinedRecognizers();
		}

		/**
		* \brief Returns the FUBI id of the user standing closest to the sensor (x-z plane)
		*/
		public static UInt32 getClosestUserID()
		{
			return FubiInternal.getClosestUserID();
		}

		/**
		* \brief Returns the number of users currently known by Fubi
		*/
		public static UInt16 getNumUsers()
		{
			return FubiInternal.getCurrentUsers(new IntPtr(0));
		}


		/**
		* \brief Convert coordinates between real world, depth, color, or IR image
		*
		* @param inputCoordsX, inputCoordsY, inputCoordsZ the coordiantes to convert
		* @param[out] outputCoordsX, outputCoordsY, outputCoordsZ vars to store the converted coordinates to
		* @param inputType the type of the inputCoords
		* @param outputType the type to convert the inputCoords to
		*/
		public static void convertCoordinates(float inputCoordsX, float inputCoordsY, float inputCoordsZ, out float outputCoordsX, out float outputCoordsY, out float outputCoordsZ,
			FubiUtils.CoordinateType inputType, FubiUtils.CoordinateType outputType)
		{
			FubiInternal.convertCoordinates(inputCoordsX, inputCoordsY, inputCoordsZ, out outputCoordsX, out outputCoordsY, out outputCoordsZ, inputType, outputType);
		}

		/**
		* \brief resests the tracking of all users
		*/
		public static void resetTracking()
		{
			FubiInternal.resetTracking();
		}

		/**
		* \brief get time since program start in seconds
		*/
		public static double getCurrentTime()
		{
			return FubiInternal.getCurrentTime();
		}

		/**
		* \brief get the filtering options for smoothing the skeleton according to the 1 Euro filter (still possible to get the unfiltered data)
		*
		* @param[out] minCutOffFrequency the minimum cutoff frequency for low pass filtering (=cut off frequency for a still joint)
		* @param[out] velocityCutOffFrequency the cutoff frequency for low pass filtering the velocity
		* @param[out] cutOffSlope how fast a higher velocity will higher the cut off frequency (->apply less smoothing with higher velocities)
		*/
		public static void getFilterOptions(out float minCutOffFrequency, out float velocityCutOffFrequency, out float cutOffSlope)
		{
			FubiInternal.getFilterOptions(out minCutOffFrequency, out velocityCutOffFrequency, out cutOffSlope);
		}

		/**
		* \brief set the filtering options for smoothing the skeleton according to the 1 Euro filter (still possible to get the unfiltered data)
		*
		* @param minCutOffFrequency (=1.0f) the minimum cutoff frequency for low pass filtering (=cut off frequency for a still joint)
		* @param velocityCutOffFrequency (=1.0f) the cutoff frequency for low pass filtering the velocity
		* @param cutOffSlope (=0.007f) how fast a higher velocity will higher the cut off frequency (->apply less smoothing with higher velocities)
		*/
		public static void setFilterOptions(float minCutOffFrequency = 1.0f, float velocityCutOffFrequency = 1.0f, float cutOffSlope = 0.007f)
		{
			FubiInternal.setFilterOptions(minCutOffFrequency, velocityCutOffFrequency, cutOffSlope);
		}

		/**
		* \brief Returns the ids of all users order by their distance to the sensor (x-z plane)
		* Closest user is at the front, user with largest distance or untracked users at the back
		* 
		* @param userIDs an array big enough to receive the indicated number of user ids (Fubi::MaxUsers at max)
		* @param maxNumUsers if greater than -1, the given number of closest users is additionally ordered from left to right position
		* @return the actual number of user ids written into the array
		*/
		public static uint getClosestUserIDs(uint[] userIDs, int maxNumUsers = -1)
		{
			return FubiInternal.getClosestUserIDs(userIDs, maxNumUsers);
		}

		/**
		* \brief Set the current tracking info of one user
		*
		* @param userID id of the user
		* @param data i.e. NUM_JOINTS * (position+orientation) with position, orientation all as 4 floats (x,y,z,conf) in milimeters or degrees
		*/
		public static void updateTrackingData(uint userID, FubiTrackingData data)
		{
			if (data != null)
			{
				double timeStamp = data.TimeStamp;
				var hSkeleton = GCHandle.Alloc(data.getArray(), GCHandleType.Pinned);
				FubiInternal.updateTrackingData(userID, hSkeleton.AddrOfPinnedObject(), timeStamp);
				hSkeleton.Free();
			}
		}

		/**
		* \brief Creates a user defined angular movement recognizer
		* 
		* @param joint the joint of interest
		* @param minVelX, minVelY, minVelZ the minimum angular velocity per axis (also defines the rotation direction)
		* @param maxVelX, maxVelY, maxVelZ the maximum angular velocity per axis (also defines the rotation direction)
		* @param useLocalOrients whether local ("substracted" parent orientation = the actual joint orientation, not the orientation in space)
		*		  or global orientations should be used
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name name of the recognizer
		* @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static uint addAngularMovementRecognizer(FubiUtils.SkeletonJoint joint,
			float minVelX = -FubiUtils.Math.MaxFloat, float minVelY = -FubiUtils.Math.MaxFloat, float minVelZ = -FubiUtils.Math.MaxFloat,
			float maxVelX = FubiUtils.Math.MaxFloat, float maxVelY = FubiUtils.Math.MaxFloat, float maxVelZ = FubiUtils.Math.MaxFloat,
			bool useLocalOrients = true,
			int atIndex = -1, string name = null,
			float minConfidence = -1.0f, bool useFilteredData = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			var ret = FubiInternal.addAngularMovementRecognizer(joint, minVelX, minVelY, minVelZ,
				maxVelX, maxVelY, maxVelZ, useLocalOrients,
				atIndex, namePtr, minConfidence, useFilteredData);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Creates a user defined template recognizer
		*
		* @param joint the joint of interest
		* @param relJoint additional joint, the previous one is taken in relation to (NUM_JOINTS for global coordinates)
		* @param trainingDataFile file containing recorded skeleton data to be used as the template for the gesture
		* @param startFrame first frame index used in the trainingDataFile
		* @param endFrame last frame index used in the trainingDataFile
		* @param maxDistance the maximum distance allowed for gestures in comparison to the template
		* @param distanceMeasure (=Euclidean) masure used to calculate the distance to the template
		* @param maxRotation (=45°) how much the gesture is allowed to be rotated to match the template better (determines level of rotation invariance)
		* @param aspectInvariant (=false) if set to true scale normalization will NOT keep the original aspect ratio increasing scale invariance 
		*		 in the sense that a square can no longer be distinguished from other rectangles
		* @param ignoreAxes (=NONE) can combine a flag for each axis that it should be ignored. Only active axes are taken for comparing the gestures
		* @param useOrientations (=false) if set to true orientation data instead of positional dat will be compared
		* @param useDTW (=true) if set to true, dynamic time warping (DTW) will be applied to find the optimal warping path to fit the gesture to the template
		* @param applyResampling (=false) if set to true, gesture candidate paths will be resampled to the same length first
		* @param stochasticModel (=GMR) by default, a gaussian mixture model will be created out of the training samples and regression will be applied to obtain general mean and covariance values,
		*        if set to HMM, hidden markov models will be used for recognition, if set to NONE, only mean an covariance matrix will be calculated
		* @param numGMRStates (=5) defines the number of states which will be used to create the GMM. the training data will be split into numGMRStates parts of equal length
		* @param measuringUnit (=NUM_MEASUREMENTS) measuring unit for positional data
		* @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		* @param name (=0) name of the recognizer
		* @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		* @param useLocalTransformations (=false) whether local or global transformations should be used
		* @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		*
		* @return index of the recognizer needed to call it later
		*/
		public static uint addTemplateRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
            string trainingDataFile, int startFrame, int endFrame,
            float maxDistance,
            FubiUtils.DistanceMeasure distanceMeasure = FubiUtils.DistanceMeasure.Euclidean,
            float maxRotation = 45.0f,
            bool aspectInvariant = false,
            uint ignoreAxes = (uint)FubiUtils.CoordinateAxis.NONE,
            bool useOrientations = false,
			bool useDTW = true,
			bool applyResampling = false,
			FubiUtils.StochasticModel stochasticModel = FubiUtils.StochasticModel.GMR,
            uint numGMRStates = 5,
            FubiUtils.BodyMeasurement measuringUnit = FubiUtils.BodyMeasurement.NUM_MEASUREMENTS,
            int atIndex = -1, string name = "",
            float minConfidence = -1.0f,
            bool useLocalTransformations = false,
            bool useFilteredData = false)
        {
            var namePtr = Marshal.StringToHGlobalAnsi(name);
            var filePtr = Marshal.StringToHGlobalAnsi(trainingDataFile);
            var ret = FubiInternal.addTemplateRecognizer(joint, relJoint, filePtr, startFrame, endFrame, 
                maxDistance, distanceMeasure, maxRotation, aspectInvariant, ignoreAxes, useOrientations, useDTW, applyResampling,
				stochasticModel, numGMRStates, measuringUnit, atIndex, namePtr, minConfidence, useLocalTransformations, useFilteredData);
            Marshal.FreeHGlobal(namePtr);
            Marshal.FreeHGlobal(filePtr);
            return ret;
        }

        /**
        * \brief Creates a user defined template recognizer
        *
        * @param xmlDefinition string containing the xml definition of the recognizer
        * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
        *
        * @return index of the recognizer needed to call it later
        */
        public static uint addTemplateRecognizer(string xmlDefinition, int atIndex = -1)
        {
            var dataPtr = Marshal.StringToHGlobalAnsi(xmlDefinition);
            var ret = FubiInternal.addTemplateRecognizer(dataPtr, atIndex);
            Marshal.FreeHGlobal(dataPtr);
            return ret;
        }

		/**
		* \brief Checks a user defined combination recognizer for its current state
		* 
		* @param recognizerName name of the combination
		* @param userID the user id of the user to be checked
		* @param[out] numStates the full number of states of this recognizer
		* @param[out] isInterrupted whether the recognizers of the current state are temporarly interrupted
		* @param[out] isInTransition if the state has already passed its min duration and would be ready to transit to the next state
		* @return number of current state (0..numStates-1), if < 0 -> error: -1 if first state not yet started, -2 user not found, -3 recognizer not found
		*/
		public static int getCurrentCombinationRecognitionState(string recognizerName, uint userID, out uint numStates, out bool isInterrupted, out bool isInTransition)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.getCurrentCombinationRecognitionState(namePtr, userID, out numStates, out isInterrupted, out isInTransition);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief initalizes a finger sensor such as the leap motion for tracking fingers
		* 
		* @param type the sensor type (see FingerSensorType definition)
		* @param offsetPosX, offsetPosY, offsetPosZ position of the finger sensor in relation to a second sensor (e.g. the Kinect) to align the coordinate systems
		* @return true if successful initialized
		*/
		public static bool initFingerSensor(FubiUtils.FingerSensorType type, float offsetPosX = 0, float offsetPosY = -600.0f, float offsetPosZ = 200.0f)
		{
			return FubiInternal.initFingerSensor(type, offsetPosX, offsetPosY, offsetPosZ);
		}

		/**
		* \brief Get the currently available finger sensor types (defined in FubiConfig.h before compilation)
		* 
		* @return an int composed of the currently available sensor types (see FingerSensorType enum for the meaning)
		*/
		public static int getAvailableFingerSensorTypes()
		{
			return FubiInternal.getAvailableFingerSensorTypes();
		}

		/**
		* \brief Get the type of the currently active sensor
		* 
		* @return the current sensor type
		*/
		public static FubiUtils.FingerSensorType getCurrentFingerSensorType()
		{
			return FubiInternal.getCurrentFingerSensorType();
		}

		/**
		* \brief Returns the number of currently tracked hands
		* 
		* @return the current number of hands
		*/
		public static ushort getNumHands()
		{
			return FubiInternal.getNumHands();
		}

		/**
		* \brief Returns the hand id from the user index
		* 
		* @param index index of the hand in the hand array
		* @return hand id of that user or 0 if not found
		*/
		public static uint getHandID(uint index)
		{
			return FubiInternal.getHandID(index);
		}

		/**
		* \brief Get the most current hand joint position
		*
		* @param handId
		* @param joint the considered joint id
		* @param[out] x, y, z where the position of the joint will be copied to
		* @param[out] confidence where the confidence for this position will be copied to
		* @param[out] timeStamp where the timestamp of this tracking info will be copied to (seconds since program start)
		* @param localPosition if set to true, the function will return local position (vector from parent joint)
		* @param filteredData if true the returned data will be data smoothed by the filter configured in the sensor
		*/
		public static void getCurrentHandJointPosition(uint handId, FubiUtils.SkeletonHandJoint joint,
			out float x, out float y, out float z, out float confidence, out double timeStamp, bool localPosition = false, bool filteredData = false)
		{
			var info = FubiInternal.getCurrentFingerTrackingData(handId, filteredData);
			FubiInternal.getSkeletonHandJointPosition(info, joint, out x, out y, out z, out confidence, out timeStamp, localPosition);
		}


		/**
		* \brief  Get the most current skeleton joint orientation
		*
		* @param handId
		* @param joint the considered joint id
		* @param[out] mat rotation 3x3 matrix (9 floats)
		* @param[out] confidence the confidence for this position
		* @param[out] timeStamp (seconds since program start)
		* @param localOrientation if set to true, the function will local orientations (cleared of parent orientation) instead of globals
		* @param filteredData if true the returned data will be data smoothed by the filter configured in the sensor
		*/
		public static void getCurrentHandJointOrientation(uint handId, FubiUtils.SkeletonHandJoint joint,
			float[] mat, out float confidence, out double timeStamp, bool localOrientation = true, bool filteredData = false)
		{
			var info = FubiInternal.getCurrentFingerTrackingData(handId, filteredData);
			if (info.ToInt32() != 0)
			{
				FubiInternal.getSkeletonHandJointOrientation(info, joint, mat, out confidence, out timeStamp, localOrientation);
			}
			else
			{
				float[] temp = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
				Array.Copy(temp, mat, temp.Length);
				confidence = 0;
				timeStamp = 0;
			}
		}

		/**
		* \brief Returns the hand id of the closest hand
		*
		* @return hand id of that hand or 0 if none found
		*/
		public static uint getClosestHandID()
		{
			return FubiInternal.getClosestHandID();
		}

		/**
		* \brief Checks a user defined gesture or posture recognizer for its success
		* 
		* @param recognizerName name of the recognizer return during its creation
		* @param handID of the hand to be checked
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult recognizeGestureOnHand(string recognizerName, uint handID)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.recognizeGestureOnHand(namePtr, handID);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Checks a user defined combination recognizer for its progress
		* 
		* @param recognizerName name of the combination
		* @param handID of the hand to be checked
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOnHand(string recognizerName, uint handID, bool restart = true)
		{
			FubiUtils.RecognitionCorrectionHint hint;
			return getCombinationRecognitionProgressOnHand(recognizerName, handID, out hint, restart);
		}

		/**
		* \brief Checks a user defined combination recognizer for its progress
		* 
		* @param recognizerName name of the combination
		* @param handID of the hand to be checked
		* @param[out] correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		* @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		* @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		*/
		public static FubiUtils.RecognitionResult getCombinationRecognitionProgressOnHand(string recognizerName, uint handID, out FubiUtils.RecognitionCorrectionHint correctionHint, bool restart = true)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var hint = new FubiUtils.RecognitionCorrectionHint();
			var ret = FubiInternal.getCombinationRecognitionProgressOnHand(namePtr, handID, new IntPtr(0), restart, false, hint);
			correctionHint = hint;
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Starts or stops the recognition process of a user defined combination for one hand
		* 
		* @param combinationName name defined for this recognizer
		* @param handID of the hand for which the recognizers should be modified
		* @param enable if set to true, the recognizer will be started (if not already stared), else it stops
		*/
		public static void enableCombinationRecognitionHand(string combinationName, uint handID, bool enable)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(combinationName);
			FubiInternal.enableCombinationRecognitionHand(namePtr, handID, enable);
			Marshal.FreeHGlobal(namePtr);
		}

		/**
		* \brief Checks a user defined combination recognizer for its current state
		* 
		* @param recognizerName name of the combination
		* @param handID of the hand to be checked
		* @param[out] numStates the full number of states of this recognizer
		* @param[out] isInterrupted whether the recognizers of the current state are temporarly interrupted
		* @param[out] isInTransition if the state has already passed its min duration and would be ready to transit to the next state
		* @return number of current state (0..numStates-1), if < 0 -> error: -1 if first state not yet started, -2 user not found, -3 recognizer not found
		*/
		public static int getCurrentCombinationRecognitionStateForHand(string recognizerName, uint handID, out uint numStates, out bool isInterrupted, out bool isInTransition)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.getCurrentCombinationRecognitionStateForHand(namePtr, handID, out numStates, out isInterrupted, out isInTransition);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		 * \brief Get the offset position of the current finger sensor to the main sensor
		 * 
		 * @param[out] xOffset, yOffset, zOffset a vector from the main sensor to the finger sensor, (0,0,0) if no sensor present
		 */
		public static void getFingerSensorOffsetPosition(out float xOffset, out float yOffset, out float zOffset)
		{
			FubiInternal.getFingerSensorOffsetPosition(out xOffset, out yOffset, out zOffset);
		}

		/**
		 * \brief Set the offset position of the current finger sensor to the main sensor
		 * 
		 * @param[out] xOffset, yOffset, zOffset the vector from the main sensor to the finger sensor
		 */
		public static void setFingerSensorOffsetPosition(float xOffset, float yOffset, float zOffset)
		{
			FubiInternal.setFingerSensorOffsetPosition(xOffset, yOffset, zOffset);
		}

		/**
		 * \brief Get the target sensor of a user defined combination recognizer
		 * 
		 * @param recognizerName name of the combination
		 * @return the target sensor as defined in FubiUtils.h
		 */
		public static FubiUtils.RecognizerTarget getCombinationRecognizerTargetSkeletonType(string recognizerName)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.getCombinationRecognizerTargetSkeleton(namePtr);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		 * \brief Get the target sensor for a recognizer
		 * 
		 * @param recognizerName name of the recognizer
		 * @return the target sensor as defined in FubiUtils.h
		 */
		public static FubiUtils.RecognizerTarget getRecognizerTargetSkeleton(string recognizerName)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var ret = FubiInternal.getRecognizerTargetSkeleton(namePtr);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		 * \brief Get meta information of a state of one recognizers
		 * 
		 * @param recognizerName name of the combination
		 * @param stateIndex the state index to get the meta info from
		 * @param propertyName the name of the property to get
		 * @return the value of the requested meta info property as a string, or 0x0 on error
		 */
		public static string getCombinationRecognitionStateMetaInfo(string recognizerName, uint stateIndex, string propertyName)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(recognizerName);
			var propertyPtr = Marshal.StringToHGlobalAnsi(propertyName);
			var info = Marshal.PtrToStringAnsi(FubiInternal.getCombinationRecognitionStateMetaInfo(namePtr, stateIndex, propertyPtr));
			Marshal.FreeHGlobal(namePtr);
			Marshal.FreeHGlobal(propertyPtr);
			return info;
		}

		/**
		* \brief Get the floor plane provided by some sensors in the Hesse normal form.
		*		 If not supported by the sensor, the normal will have length 0, else 1.
		* @param[out] normalX, normalY, normalZ the unit normal vector that points from the origin of the coordinate system to the plane
		* @param[out] dist the distance from the origin to the plane
		*/
		public static void getFloorPlane(out float normalX, out float normalY, out float normalZ, out float dist)
		{
			FubiInternal.getFloorPlane(out normalX, out normalY, out normalZ, out dist);
		}

		/**
		* \brief Start recording tracking data to a file
		*
		* @param fileName name of the file to write to (note: if it already exists, all contents will be overwritten!)
		* @param targetID id of the hand or user to be recorded
		* @param useFilteredData (=false) if true, filtered tracking data instead of the raw one will be written
		* @param isHand (=false) whether to record a hand or user
		*
		* @return true if opening the file was successful and all paramters are valid
		*/
		public static bool startRecordingSkeletonData(string fileName, uint targetID, bool useFilteredData = false, bool isHand = false)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(fileName);
			var ret = FubiInternal.startRecordingSkeletonData(namePtr, targetID, useFilteredData, isHand);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Check whether a recording process is currently running
		* 
		* @return whether a recording process is currently running
		*/
		public static bool isRecordingSkeletonData()
		{
			var currentFrameID = 0;
			return isRecordingSkeletonData(ref currentFrameID);
		}

		/**
		* \brief Check whether a recording process is currently running
		* 
		* @param currentFrameID last frame id that has already been feed into Fubi
		* @return whether a recording process is currently running
		*/
		public static bool isRecordingSkeletonData(ref int currentFrameID)
		{
			return FubiInternal.isRecordingSkeletonData(ref currentFrameID);
		}

		/**
		* \brief Stop previously started recording of tracking data and close the corresponding file
		*/
		public static void stopRecordingSkeletonData()
		{
			FubiInternal.stopRecordingSkeletonData();
		}

		/**
		* \brief Load tracking data from a previously recorded file for later playback
		*
		* @param fileName name of the file to play back
		* @return number of loaded frames, -1 if something went wrong
		*/
		public static int loadRecordedSkeletonData(string fileName)
		{
			var namePtr = Marshal.StringToHGlobalAnsi(fileName);
			var ret = FubiInternal.loadRecordedSkeletonData(namePtr);
			Marshal.FreeHGlobal(namePtr);
			return ret;
		}

		/**
		* \brief Set the playback markers of a previously loaded recording
		*
		* @param currentFrame the current playback marker
		* @param startFrame the first frame to be played
		* @param endFrame (=-1) the last frame to be played (-1 for the end of file)
		*/
		public static void setPlaybackMarkers(int currentFrame, int startFrame = 0, int endFrame = -1)
		{
			FubiInternal.setPlaybackMarkers(currentFrame, startFrame, endFrame);
		}

		/**
		* \brief Get the current playback markers of a previously loaded recording
		*
		* @param[out] currentFrame the current playback marker
		* @param[out] startFrame the first frame to be played
		* @param[out] endFrame the last frame to be played
		*/
		public static void getPlaybackMarkers(ref int currentFrame, ref int startFrame, ref int endFrame)
		{
			FubiInternal.getPlaybackMarkers(ref currentFrame, ref startFrame, ref endFrame);
		}

		/**
		* \brief Start play back of a previously loaded recording
		*
		* @param loop (=false) if true, playback restarts when the endframe is reached
		*/
		public static void startPlayingSkeletonData(bool loop = false)
		{
			FubiInternal.startPlayingSkeletonData(loop);
		}

        /**
		* \brief Check whether the play back of a recording is still running
		*
		* @return whether the play back of a recording is still running
		*/
        public static bool isPlayingSkeletonData()
        {
            var currentFrameID = 0;
	        var isPaused = false;
            return FubiInternal.isPlayingSkeletonData(ref currentFrameID, ref isPaused);
        }

		/**
		* \brief Check whether the play back of a recording is still running
		*
		* @param currentFrameID last frame id that has already been feed into Fubi
		* @return whether the play back of a recording is still running
		*/
		public static bool isPlayingSkeletonData(ref int currentFrameID)
		{
			var isPaused = false;
			return FubiInternal.isPlayingSkeletonData(ref currentFrameID, ref isPaused);
		}

		/**
		* \brief Check whether the play back of a recording is still running
		*
		* @param currentFrameID last frame id that has already been feed into Fubi
		* @param isPaused whether the playback is paused at the current frame
		* @return whether the play back of a recording is still running
		*/
		public static bool isPlayingSkeletonData(ref int currentFrameID, ref bool isPaused)
		{
			return FubiInternal.isPlayingSkeletonData(ref currentFrameID, ref isPaused);
		}

		/**
		* \brief Stop previously started playing of tracking data
		*/
		public static void stopPlayingRecordedSkeletonData()
		{
			FubiInternal.stopPlayingRecordedSkeletonData();
		}

		/**
		* \brief Pause previously started playing of tracking data (keeps the current user in the scene)
		*/
		public static void pausePlayingRecordedSkeletonData()
		{
			FubiInternal.pausePlayingRecordedSkeletonData();
		}

		/**
		* \brief Get the playback duration for the currently set start and end markers
		*
		* @return playback duration in seconds
		*/
		public static double getPlaybackDuration()
		{
			return FubiInternal.getPlaybackDuration();
		}

		/**
		* \brief Trim the previously loaded playback file to the currently set markers
		*
		* @return true if successful
		*/
		public static bool trimPlaybackFileToMarkers()
		{
			return FubiInternal.trimPlaybackFileToMarkers();	
		}

		/** 
		 * \brief Delegate type to handle recognition events
		 */
		public delegate void RecognitionHandler(string gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType);
		
		/**
		 * \brief The private event handlers, will be encapsulated by public properties
		 */
		private static event RecognitionHandler s_recognitionStart, s_recognitionEnd;

		/** 
		 * \brief Recognition start event
		 * */
		public static event RecognitionHandler RecognitionStart
		{
			add
			{
				if (s_recognitionStart == null)
				{
					if (s_recognitionEnd != null)
						FubiInternal.setRecognitionCallbacks(s_recognitionStartDelegate, s_recognitionEndDelegate);
					else
						FubiInternal.setRecognitionCallbacks(s_recognitionStartDelegate, null);
				}
				s_recognitionStart += value;
			}
			remove
			{
				s_recognitionStart -= value;
				if (s_recognitionStart == null)
				{
					if (s_recognitionEnd != null)
						FubiInternal.setRecognitionCallbacks(null, s_recognitionEndDelegate);
					else
						FubiInternal.setRecognitionCallbacks(null, null);
				}
			}
		}

		/** 
		 * \brief Recognition end event
		 * */
		public static event RecognitionHandler RecognitionEnd
		{
			add
			{
				if (s_recognitionEnd == null)
				{
					if (s_recognitionStart != null)
						FubiInternal.setRecognitionCallbacks(s_recognitionStartDelegate, s_recognitionEndDelegate);
					else
						FubiInternal.setRecognitionCallbacks(null, s_recognitionEndDelegate);
				}
				s_recognitionEnd += value;
			}
			remove
			{
				s_recognitionEnd -= value;
				if (s_recognitionEnd == null)
				{
					if (s_recognitionStart != null)
						FubiInternal.setRecognitionCallbacks(s_recognitionStartDelegate, null);
					else
						FubiInternal.setRecognitionCallbacks(null, null);
				}
			}
		}

		/**
		 * \brief The private recognition callback delegates holding pointers to the below functions
		 */
		private static event FubiInternal.RecognitionCallbackDelegate s_recognitionStartDelegate = new FubiInternal.RecognitionCallbackDelegate(onRecognitionStart),
			s_recognitionEndDelegate = new FubiInternal.RecognitionCallbackDelegate(onRecognitionEnd);

		/**
		* \brief The private on recognition start method will be called by the C++ API in case a recognition started
		*/
		private static void onRecognitionStart(IntPtr gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
		{
			string name = Marshal.PtrToStringAnsi(gestureName);
			if (s_recognitionStart != null)
				s_recognitionStart(name, targetID, isHand, recognizerType);
		}

		/**
		* \brief The private on recognition end method will be called by the C++ API in case a recognition ended
		*/
		private static void onRecognitionEnd(IntPtr gestureName, uint targetID, bool isHand, FubiUtils.RecognizerType recognizerType)
		{
			string name = Marshal.PtrToStringAnsi(gestureName);
			if (s_recognitionEnd != null)
				s_recognitionEnd(name, targetID, isHand, recognizerType);
		}

		/**
		* \brief retrieve an image from one of the finger sensor streams with specific format
		*
		* @param[out] outputImage pointer to a unsigned char array
		*        Will be filled with wanted image
		*		  Array has to be of correct size, i.e. width*height*bytesPerPixel*numChannels
		* @param image index index of the image stream, number of streams can be retrieved with getFingerSensorImageConfig
		* @param numChannels number channels in the image 1, 3 or 4
		* @param depth the pixel depth of the image, 8 bit (standard), 16 bit (mainly usefull for depth images) or 32 bit float
		*/
		public static bool getImage(byte[] outputImage, uint imageIndex, FubiUtils.ImageNumChannels numChannels, FubiUtils.ImageDepth depth)
		{
			var ret = false;
			if (outputImage != null)
			{
				var h = GCHandle.Alloc(outputImage, GCHandleType.Pinned);
				ret = FubiInternal.getImage(h.AddrOfPinnedObject(), imageIndex, numChannels, depth);
				h.Free();
			}
			return ret;
		}

		/**
		* \brief get the image width, height and number of streams of the current finger sensor
		*        assumes that all streams have the same width and height
		*
		* @param[out] width image width
		* @param[out] height image width
		* @param[out] numStreams number of image streams provided by the current finger sensor
		*/
		public static void getFingerSensorImageConfig(out int width, out int height, out uint numStreams)
		{
			FubiInternal.getFingerSensorImageConfig(out width, out height, out numStreams);
		}
		/*! @}*/
	}
}

