
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace FubiNET
{
	internal static class FubiInternal
	{
#if DEBUG
	   private const string DllName = "Fubid.dll";
#else
	   private const string DllName = "Fubi.dll";
#endif
	   /**
		 * \brief Initializes Fubi with OpenN 1.x using the given xml file and sets the skeleton profile.
		 *        If no xml file is given, Fubi will be intialized without OpenNI tracking enabled --> methods that need an openni context won't work.
		 * 
		 * @param openniXmlconfig name of the xml file for openNI initialization inlcuding all needed productions nodes 
			(should be placed in the working directory, i.e. "bin" folder)
			if config == 0x0, then OpenNI won't be initialized and Fubi stays in non-tracking mode
		 * @param profile set the openNI skeleton profile
		 * @param filter... options for filtering the tracking data if wanted
		 * @param mirrorStream whether the stream should be mirrored or not
		 * @param registerStreams whether the depth stream should be registered to the color stream
		 * @return true if succesfully initialized or already initialized before,
			false means bad xml file or other serious problem with OpenNI initialization
		 *
		 * Default openni xml configuration file (note that only specific resolutions and FPS values are allowed):
			<OpenNI>
			  <Log writeToConsole="true" writeToFile="false">
				<!-- 0 - Verbose, 1 - Info, 2 - Warning, 3 - Error (default) -->
				<LogLevel value="2"/>
			  </Log>
			  <ProductionNodes>
				<Node type="Image">
				  <Configuration>
					<MapOutputMode xRes="1280" yRes="1024" FPS="15"/>
					<Mirror on="true"/>
				  </Configuration>
				</Node>
				<Node type="Depth">
				  <Configuration>
					<MapOutputMode xRes="640" yRes="480" FPS="30"/>
					<Mirror on="true"/>
				  </Configuration>
				</Node>
				<Node type="Scene" />
				<Node type="User" />
			  </ProductionNodes>
			</OpenNI>
		 */
	   [DllImport(DllName, EntryPoint = "?init@Fubi@@YA_NPBDW4Profile@SkeletonTrackingProfile@1@MMM_N2@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
	   internal static extern bool init(IntPtr openniXmlconfig, FubiUtils.SkeletonProfile profile,
		   float filterMinCutOffFrequency = 1.0f, float filterVelocityCutOffFrequency = 1.0f, float filterCutOffSlope = 0.007f,
		   [MarshalAs(UnmanagedType.U1)]bool mirrorStreams = true, [MarshalAs(UnmanagedType.U1)]bool registerStreams = true);

		/**
		 * \brief Initializes Fubi with specific options for the sensor init
		 * 
		 * @param depthWidth, ... options configuration for the sensor as in the SensorOptions struct
		 * @return true if succesfully initialized or already initialized before,
			false means problem with sensor init
			*/
		[DllImport(DllName, EntryPoint = "?init@Fubi@@YA_NHHHHHHHHHW4Type@SensorType@1@W4Profile@SkeletonTrackingProfile@1@MMM_N2@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool init(int depthWidth, int depthHeight, int depthFps = 30,
			int rgbWidth = 640, int rgbHeight = 480, int rgbFps = 30,
			int irWidth = -1, int irHeight = -1, int irFps = -1,
			FubiUtils.SensorType type = FubiUtils.SensorType.OPENNI2,
			FubiUtils.SkeletonProfile profile = FubiUtils.SkeletonProfile.ALL,
			float filterMinCutOffFrequency = 1.0f, float filterVelocityCutOffFrequency = 1.0f, float filterCutOffSlope = 0.007f,
			[MarshalAs(UnmanagedType.U1)]bool mirrorStream = true, [MarshalAs(UnmanagedType.U1)]bool registerStreams = true);

		/**
			* \brief Allows you to switch between different sensor types during runtime
			*		  Note that this will also reinitialize most parts of Fubi
			* 
			* @param options options for initializing the new sensor
			* @return true if the sensor has been succesfully initialized
			*/
		[DllImport(DllName, EntryPoint = "?switchSensor@Fubi@@YA_NW4Type@SensorType@1@HHHHHHHHHW4Profile@SkeletonTrackingProfile@1@_N2@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool switchSensor(FubiUtils.SensorType type, int depthWidth, int depthHeight, int depthFps = 30,
			int rgbWidth = 640, int rgbHeight = 480, int rgbFps = 30,
			int irWidth = -1, int irHeight = -1, int irFps = -1,
			FubiUtils.SkeletonProfile profile = FubiUtils.SkeletonProfile.ALL,
			[MarshalAs(UnmanagedType.U1)]bool mirrorStream = true, [MarshalAs(UnmanagedType.U1)]bool registerStreams = true);

		/**
		 * \brief Get the currently available sensor types (defined in FubiConfig.h before compilation)
		 * 
		 * @return an int composed of the currently available sensor types (see SensorType enum for the meaning)
			*/
		[DllImport(DllName, EntryPoint = "?getAvailableSensorTypes@Fubi@@YAHXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern int getAvailableSensorTypes();

		/**
		 * \brief Get the type of the currently active sensor
		 * 
		 * @return the current sensor type
			*/
		 [DllImport(DllName, EntryPoint = "?getCurrentSensorType@Fubi@@YA?AW4Type@SensorType@1@XZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.SensorType getCurrentSensorType();


		/**
		 * \brief Shuts down OpenNI and the tracker, releasing all allocated memory
		 * 
		 */
		[DllImport(DllName, EntryPoint = "?release@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void release();

		/**
		  * \brief Updates the sensor to get the next frame of depth, rgb, and tracking data.
		  *        Also searches for users in the scene and loads the default tracking calibration for new users or request a calibration
		  * 
		  */
		[DllImport(DllName, EntryPoint = "?updateSensor@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void updateSensor();

		/**
		  * \brief retrieve an image from one of the OpenNI production nodes with specific format and optionally enhanced by different
		  *        tracking information 
		  *		  Some render options require an OpenCV installation!
		  *
		  * @param outputImage pointer to an unsigned char array
		  *        Will be filled with wanted image
		  *		  Array has to be of correct size, e.g. depth image (640x480 std resolution) with tracking info
		  *		  requires 4 channels (RGBA) --> size = 640*480*4 = 1228800
		  * @param type can be color, depth, or ir image
		  * @param numChannels number channels in the image 1, 3 or 4
		  * @param depth the pixel depth of the image, 8 bit (standard) or 16 bit (mainly usefull for depth images
		  * @param renderOptions options for rendering additional informations into the image (e.g. tracking skeleton)
		  * @param jointsToRender defines for which of the joints the trackinginfo (see renderOptions) should be rendererd
		  * @param depthModifications options for transforming the depht image to a more visible format
		  * @param userID If set to something else than 0 an image will be cut cropped around (the joint of interest of) this user, if 0 the whole image is put out.
		  * @param jointOfInterest the joint of the user the image is cropped around and a threshold on the depth values is applied.
			  If set to num_joints fubi tries to crop out the whole user.
		  * @param moveCroppedToUpperLeft moves the cropped image to the upper left corner
		 */
		[DllImport(DllName, EntryPoint = "?getImage@Fubi@@YA_NPAEW4Type@ImageType@1@W4Channel@ImageNumChannels@1@W4Depth@ImageDepth@1@HHW4Modification@DepthImageModification@1@IW4Joint@SkeletonJoint@1@_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getImage(IntPtr outputImage, FubiUtils.ImageType type, FubiUtils.ImageNumChannels numChannels, FubiUtils.ImageDepth depth, 
			int renderOptions = (int)FubiUtils.RenderOptions.Default,
			int jointsToRender = (int)FubiUtils.JointsToRender.ALL_JOINTS,
			FubiUtils.DepthImageModification depthModifications = FubiUtils.DepthImageModification.UseHistogram,
			UInt32 userID = 0, FubiUtils.SkeletonJoint jointOfInterest = FubiUtils.SkeletonJoint.NUM_JOINTS, [MarshalAs(UnmanagedType.U1)]bool moveCroppedToUpperLeft = false);


		/**
		 * \brief Tries to recognize a posture in the current frame of tracking data of one user
		 * 
		 * @param postureID enum id of the posture to be found in FubiPredefinedGestures.h
		 * @param userID the OpenNI user id of the user to be checked
		 * @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		 */
		[DllImport(DllName, EntryPoint = "?recognizeGestureOn@Fubi@@YA?AW4Result@RecognitionResult@1@W4Posture@Postures@1@I@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult recognizeGestureOn(FubiPredefinedGestures.Postures postureID, UInt32 userID);

		/**
		 * \brief Checks a user defined gesture or posture recognizer for its success
		 * 
		 * @param recognizerID id of the recognizer return during its creation
		 * @param userID the OpenNI user id of the user to be checked
		 * @return true in case of a succesful detection
		 */
		[DllImport(DllName, EntryPoint = "?recognizeGestureOn@Fubi@@YA?AW4Result@RecognitionResult@1@IIPAURecognitionCorrectionHint@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult recognizeGestureOn(UInt32 recognizerIndex, UInt32 userID, [In, Out] FubiUtils.RecognitionCorrectionHint hint = null);

		/**
		 * \brief Returns the OpenNI user id from the user index
		 * 
		 * @param index index of the user in the user array
		 * @return OpenNI user id of that user or 0 if not found
		 */
		[DllImport(DllName, EntryPoint = "?getUserID@Fubi@@YAII@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 getUserID(UInt32 index);

		/**
		 * \brief Automatically starts combination recogntion for new users
		 * 
		 * @param enable if set to true, the recognizer will automatically start for new users, else this must be done manually (by using enableCombinationRecognition(..))
		 * @param combinationID enum id of the combination to be found in FubiPredefinedGestures.h or NUM_COMBINATIONS for all combinations
		 */
		[DllImport(DllName, EntryPoint = "?setAutoStartCombinationRecognition@Fubi@@YAX_NW4Combination@Combinations@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void setAutoStartCombinationRecognition([MarshalAs(UnmanagedType.U1)]bool enable, FubiPredefinedGestures.Combinations combinationID = FubiPredefinedGestures.Combinations.NUM_COMBINATIONS);


		/**
		 * \brief Check if autostart is activated for a combination recognizer
		 * 
		 * @param combinationID enum id of the combination to be found in FubiPredefinedGestures.h or NUM_COMBINATIONS for all combinations
		 * @return true if the corresponding auto start is activated
		 */
		[DllImport(DllName, EntryPoint = "?getAutoStartCombinationRecognition@Fubi@@YA_NW4Combination@Combinations@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool getAutoStartCombinationRecognition(FubiPredefinedGestures.Combinations combinationID = FubiPredefinedGestures.Combinations.NUM_COMBINATIONS);

		/**
		 * \brief Starts or stops the recognition process of a combination for one user
		 * 
		 * @param combinationID enum id of the combination to be found in FubiPredefinedGestures.h
		 * @param userID the OpenNI user id of the user for whom the recognizers should be modified
		 * @param enable if set to true, the recognizer will be started (if not already stared), else it stops
		 */
		[DllImport(DllName, EntryPoint = "?enableCombinationRecognition@Fubi@@YAXW4Combination@Combinations@1@I_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void enableCombinationRecognition(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, [MarshalAs(UnmanagedType.U1)]bool enable);

		/**
		 * \brief Checks a combination recognizer for its success
		 * 
		 * @param combinationID  enum id of the combination to be found in FubiPredefinedGestures.h
		 * @param userID the OpenNI user id of the user to be checked
		 * @param userStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		 *		  during the recognition of each state
		 * @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		 * @param returnFilteredData if true, the user states vector will contain filtered data
		 * @param correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		 * @return true in case of a succesful detection
		 */
		[DllImport(DllName, EntryPoint = "?getCombinationRecognitionProgressOn@Fubi@@YA?AW4Result@RecognitionResult@1@W4Combination@Combinations@1@IPAV?$vector@UTrackingData@Fubi@@V?$allocator@UTrackingData@Fubi@@@std@@@std@@_N2PAURecognitionCorrectionHint@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(FubiPredefinedGestures.Combinations combinationID, UInt32 userID, IntPtr userStates, [MarshalAs(UnmanagedType.U1)]bool restart = true, [MarshalAs(UnmanagedType.U1)]bool returnFilteredData = false, [In, Out] FubiUtils.RecognitionCorrectionHint hint = null);

		/**
		 * \brief Returns true if OpenNI has been already initialized
		 * 
		 */
		[DllImport(DllName, EntryPoint = "?isInitialized@Fubi@@YA_NXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool isInitialized();

		/**
		* \brief Returns the current depth resolution
		* 
		* @param width, height the resolution
		*/
		[DllImport(DllName, EntryPoint = "?getDepthResolution@Fubi@@YAXAAH0@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getDepthResolution(out Int32 width, out Int32 height);

		/**
		 * \brief Returns the current rgb resolution
		 * 
		 * @param width, height the resolution
		 */
		[DllImport(DllName, EntryPoint = "?getRgbResolution@Fubi@@YAXAAH0@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getRgbResolution(out Int32 width, out Int32 height);

		/**
		 * \brief Returns the current rgb resolution
		 * 
		 * @param width, height the resolution
		 */
		[DllImport(DllName, EntryPoint = "?getIRResolution@Fubi@@YAXAAH0@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getIRResolution(out Int32 width, out Int32 height);


		 /**
		 * \brief Loads a recognizer config xml file and adds the configured recognizers
		 * 
		 * @para fileName name of the xml config file
		 * @return true if at least one recognizers was loaded from the given xml
		 */
		[DllImport(DllName, EntryPoint = "?loadRecognizersFromXML@Fubi@@YA_NPBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool loadRecognizersFromXML(IntPtr fileName);

		/**
		 * \brief Returns current number of user defined recognizers
		 * 
		 * @return number of recognizers, the recognizers also have the indices 0 to numberOfRecs-1
		 */
		[DllImport(DllName, EntryPoint = "?getNumUserDefinedRecognizers@Fubi@@YAIXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 getNumUserDefinedRecognizers();

		/**
		 * \brief Returns the name of a user defined recognizer
		 * 
		 * @param  recognizerIndex index of the recognizer
		 * @return returns the recognizer name or an empty string if the user is not found or the name not set
		 */
		[DllImport(DllName, EntryPoint = "?getUserDefinedRecognizerName@Fubi@@YAPBDI@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getUserDefinedRecognizerName(UInt32 recognizerIndex);

		/**
		 * \brief Returns the index of a user defined recognizer
		 * 
		 * @param recognizerName name of the recognizer
		 * @return returns the recognizer name or -1 if not found
		 */
		[DllImport(DllName, EntryPoint = "?getUserDefinedRecognizerIndex@Fubi@@YAHPBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern Int32 getUserDefinedRecognizerIndex(IntPtr recognizerName);

		/**
		 * \brief Checks a user defined gesture or posture recognizer for its success
		 * 
		 * @param recognizerName name of the recognizer return during its creation
		 * @param userID the OpenNI user id of the user to be checked
		 * @return true in case of a succesful detection
		 */
		[DllImport(DllName, EntryPoint = "?recognizeGestureOn@Fubi@@YA?AW4Result@RecognitionResult@1@PBDIPAURecognitionCorrectionHint@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult recognizeGestureOn(IntPtr recognizerName, UInt32 userID, [In, Out] FubiUtils.RecognitionCorrectionHint hint = null);

		/**
		 * \brief Returns the number of shown fingers detected at the hand of one user (REQUIRES OPENCV!)
		 * 
		 * @param userID FUBI id of the user
		 * @param leftHand looks at the left instead of the right hand
		 * @param getMedianOfLastFrames uses the precalculated median of finger counts of the last frames (still calculates new one if there is no precalculation)
		 * @param medianWindowSize defines the number of last frames that is considered for calculating the median
		 * @param useConvexityDefectMethod if true using old method that calculates the convexity defects
		 * @return the number of shown fingers detected, 0 if there are none or there is an error
		 */
		[DllImport(DllName, EntryPoint = "?getFingerCount@Fubi@@YAHI_N0I0@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern Int32 getFingerCount(UInt32 userID, [MarshalAs(UnmanagedType.U1)]bool leftHand = false,
			[MarshalAs(UnmanagedType.U1)]bool getMedianOfLastFrames = true, UInt32 medianWindowSize = 10,
			[MarshalAs(UnmanagedType.U1)]bool useConvexityDefectMethod = false);

		/**
		 * \brief Extract the number of shown fingers out of the finger tracking data
		 * 
		 * @param trackingData the finger tracking data struct
		 * @return the number of shown fingers, 0 if there are none or there is an error
		 */
		[DllImport(DllName, EntryPoint = "?getHandFingerCount@Fubi@@YAHPBUTrackingData@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern int getHandFingerCount(IntPtr trackingData);

		/**
		 * \brief Checks a user defined combination recognizer for its success
		 * 
		 * @param recognizerName name of the combination
		 * @param userID the OpenNI user id of the user to be checked
		 * @param userStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		 *		  during the recognition of each state
		 * @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		 * @param returnFilteredData if true, the user states vector will contain filtered data
		 * @param correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		 * @return true in case of a succesful detection
		 */
		[DllImport(DllName, EntryPoint = "?getCombinationRecognitionProgressOn@Fubi@@YA?AW4Result@RecognitionResult@1@PBDIPAV?$vector@UTrackingData@Fubi@@V?$allocator@UTrackingData@Fubi@@@std@@@std@@_N2PAURecognitionCorrectionHint@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult getCombinationRecognitionProgressOn(IntPtr recognizerName, UInt32 userID, IntPtr userStates, [MarshalAs(UnmanagedType.U1)]bool restart = true, [MarshalAs(UnmanagedType.U1)]bool returnFilteredData = false, [In, Out] FubiUtils.RecognitionCorrectionHint hint = null);

		/**
		 * \brief Returns the index of a user defined combination recognizer
		 * 
		 * @param recognizerName name of the recognizer
		 * @return returns the recognizer name or -1 if not found
		 */
		[DllImport(DllName, EntryPoint = "?getUserDefinedCombinationRecognizerIndex@Fubi@@YAHPBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern Int32 getUserDefinedCombinationRecognizerIndex(IntPtr recognizerName);

		/**
		 * \brief Returns current number of user defined combination recognizers
		 * @return number of recognizers, the recognizers also have the indices 0 to numberOfRecs-1
		 */
		[DllImport(DllName, EntryPoint = "?getNumUserDefinedCombinationRecognizers@Fubi@@YAIXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 getNumUserDefinedCombinationRecognizers();

		/**
		 * \brief Returns the name of a user defined combination recognizer
		 * 
		 * @param  recognizerIndex index of the recognizer
		 * @return returns the recognizer name or an empty string if the user is not found or the name not set
		 */
		[DllImport(DllName, EntryPoint = "?getUserDefinedCombinationRecognizerName@Fubi@@YAPBDI@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getUserDefinedCombinationRecognizerName(UInt32 recognizerIndex);

		/**
		 * \brief Starts or stops the recognition process of a user defined combination for one user
		 * 
		 * @param combinationName name defined for this recognizer
		 * @param userID the OpenNI user id of the user for whom the recognizers should be modified
		 * @param enable if set to true, the recognizer will be started (if not already stared), else it stops
		 */
		[DllImport(DllName, EntryPoint = "?enableCombinationRecognition@Fubi@@YAXPBDI_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void enableCombinationRecognition(IntPtr combinationName, UInt32 userID, [MarshalAs(UnmanagedType.U1)]bool enable);

		/**
		 * \brief Returns the color for a user in the background image
		 * 
		 * @param id OpennNI user id of the user of interest
		 * @param r, g, b returns the red, green, and blue components of the color in which the users shape is displayed in the tracking image
		 *        returns 0,0,0 (black) if not found
		 */
		[DllImport(DllName, EntryPoint = "?getColorForUserID@Fubi@@YAXIAAM00@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getColorForUserID(UInt32 id, out float r, out float g, out float b);

		/**
		 * \brief save an image from one of the OpenNI production nodes with specific format and optionally enhanced by different
		 *        tracking information
		 *
		 * @param filename filename where the image should be saved to
		 *        can be relative to the working directory (bin folder) or absolute
		 *		  the file extension determins the file format (should be jpg)
		 * @param jpegQuality qualitiy (= 88) of the jpeg compression if a jpg file is requested, ranges from 0 to 100 (best quality)
		 * @param type can be color, depth, or ir image
		 * @param numChannels number channels in the image 1, 3 or 4
		 * @param depth the pixel depth of the image, 8 bit (standard) or 16 bit (mainly usefull for depth images
		 * @param renderOptions options for rendering additional informations into the image (e.g. tracking skeleton)
		 * @param jointsToRender defines for which of the joints the trackinginfo (see renderOptions) should be rendererd
		 * @param depthModifications options for transforming the depht image to a more visible format
		 * @param userID If set to something else than 0 an image will be cut cropped around (the joint of interest of) this user, if 0 the whole image is put out.
		 * @param jointOfInterest the joint of the user the image is cropped around and a threshold on the depth values is applied.
				  If set to num_joints fubi tries to crop out the whole user.
		*/

		[DllImport(DllName, EntryPoint = "?saveImage@Fubi@@YA_NPBDHW4Type@ImageType@1@W4Channel@ImageNumChannels@1@W4Depth@ImageDepth@1@HHW4Modification@DepthImageModification@1@IW4Joint@SkeletonJoint@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool saveImage(IntPtr fileName, int jpegQuality /*0-100*/,
			FubiUtils.ImageType type, FubiUtils.ImageNumChannels numChannels, FubiUtils.ImageDepth depth,
			int renderOptions = (int)FubiUtils.RenderOptions.Default,
			int jointsToRender = (int)FubiUtils.JointsToRender.ALL_JOINTS,
			FubiUtils.DepthImageModification depthModifications = FubiUtils.DepthImageModification.UseHistogram,
			UInt32 userID = 0, FubiUtils.SkeletonJoint jointOfInterest = FubiUtils.SkeletonJoint.NUM_JOINTS);

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
		[DllImport(DllName, EntryPoint = "?addJointRelationRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@0MMMMMMMM0MMMMMMMM_NHPBDMW4Measurement@BodyMeasurement@1@1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addJointRelationRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
			float minX, float minY, float minZ, 
			float maxX, float maxY, float maxZ, 
			float minDistance, float maxDistance,
			FubiUtils.SkeletonJoint midJoint,
			float midJointMinX, float midJointMinY, float midJointMinZ,
			float midJointMaxX, float midJointMaxY, float midJointMaxZ,
			float midJointMinDistance, float midJointMaxDistance,
			[MarshalAs(UnmanagedType.U1)] bool useLocalPositions,
			Int32 atIndex, IntPtr name,
			float minConfidence, FubiUtils.BodyMeasurement measuringUnit,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);

		/**
		 * \brief Creates a user defined linear movement recognizer
		 * 
		 * @param joint the joint of interest
		 * @param relJoint the joint in which it has to be in a specifc relation
		 * @param direction the direction in which the movement should happen
		 * @param minVel the minimal velocity that has to be reached in this direction
		 * @param maxVel (= inf) the maximal velocity that is allowed in this direction
		 * @param minLength the minimal length of path that has to be reached (only works within a combination rec)
		 * @param maxLength the maximal length of path that can be reached (only works within a combination rec)
		 * @param measuringUnit measuring uint for the path length
		 * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		 * @param name name of the recognizer
		 * @param maxAngleDifference (=45°) the maximum angle difference that is allowed between the requested direction and the actual movement direction
		 * @param bool useOnlyCorrectDirectionComponent (=true) If true, this only takes the component of the actual movement that is conform
		 *				the requested direction, else it always uses the actual movement for speed calculation
		 * @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		 *
		 * @return index of the recognizer needed to call it later
		 */
			// A linear gesture has a vector calculated as joint - relative joint, 
			// the direction (each component -1 to +1) that will be applied per component on the vector, and a min and max vel in milimeter per second
		[DllImport(DllName, EntryPoint = "?addLinearMovementRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@0MMMMM_NHPBDM11@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
			float dirX, float dirY, float dirZ, float minVel, float maxVel,
			[MarshalAs(UnmanagedType.U1)] bool useLocalPositions,
			int atIndex, IntPtr name, float maxAngleDifference, [MarshalAs(UnmanagedType.U1)] bool useOnlyCorrectDirectionComponent,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);
		[DllImport(DllName, EntryPoint = "?addLinearMovementRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@MMMMM_NHPBDM11@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint,
			float dirX, float dirY, float dirZ, float minVel, float maxVel,
			[MarshalAs(UnmanagedType.U1)] bool useLocalPositions,
			int atIndex, IntPtr name, float maxAngleDifference, [MarshalAs(UnmanagedType.U1)] bool useOnlyCorrectDirectionComponent,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);
		[DllImport(DllName, EntryPoint = "?addLinearMovementRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@0MMMMMMMW4Measurement@BodyMeasurement@1@_NHPBDM22@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
			float dirX, float dirY, float dirZ, float minVel, float maxVel,
			float minLength, float maxLength, FubiUtils.BodyMeasurement measuringUnit,
			[MarshalAs(UnmanagedType.U1)] bool useLocalPositions,
			int atIndex, IntPtr name, float maxAngleDifference, [MarshalAs(UnmanagedType.U1)] bool useOnlyCorrectDirectionComponent,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);
		[DllImport(DllName, EntryPoint = "?addLinearMovementRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@MMMMMMMW4Measurement@BodyMeasurement@1@_NHPBDM22@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addLinearMovementRecognizer(FubiUtils.SkeletonJoint joint,
			float dirX, float dirY, float dirZ, float minVel, float maxVel,
			float minLength, float maxLength, FubiUtils.BodyMeasurement measuringUnit,
			[MarshalAs(UnmanagedType.U1)] bool useLocalPositions,
			int atIndex, IntPtr name, float maxAngleDifference, [MarshalAs(UnmanagedType.U1)] bool useOnlyCorrectDirectionComponent,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);

		/**
		 * \brief Creates a user defined finger count recognizer
		 * 
		 * @param joint the hand joint of interest
		 * @param minFingers the minimum number of fingers the user should show up
		 * @param maxFingers the maximum number of fingers the user should show up
		 * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		 * @param name (= 0) sets a name for the recognizer (should be unique!)
		 * @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		 * @param useMedianCalculation (=false) if true, the median for the finger count will be calculated over several frames instead of always taking the current detection
		 * @param unsigned int medianWindowSize (=10) defines the window size for calculating the median (=number of frames)
		 * @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		 *
		 * @return index of the recognizer needed to call it later
		 */
		[DllImport(DllName, EntryPoint = "?addFingerCountRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@IIHPBDM_NI2@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		 internal static extern UInt32 addFingerCountRecognizer(FubiUtils.SkeletonJoint handJoint,
			UInt32 minFingers, UInt32 maxFingers,
			Int32 atIndex,
			IntPtr name,
			float minConfidence,
			[MarshalAs(UnmanagedType.U1)] bool useMedianCalculation,
			UInt32 medianWindowSize = 10,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);

		/**
		 * \brief Creates a user defined joint orientation recognizer
		 * 
		 * @param joint the joint of interest
		 * @param minValues (=-180, -180, -180) the minimal degrees allowed for the joint orientation
		 * @param maxValues (=180, 180, 180) the maximal degrees allowed for the joint orientation
		 * @param useLocalOrientations if true, uses a local orienation in which the parent orientation has been substracted
		 * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		 * @param name (= 0) sets a name for the recognizer (should be unique!)
		 * @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		 * @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		 *
		 * @return index of the recognizer needed to call it later
		 */
		[DllImport(DllName, EntryPoint = "?addJointOrientationRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@MMMMMM_NHPBDM1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addJointOrientationRecognizer(FubiUtils.SkeletonJoint joint,
			float minX , float minY , float minZ,
			float maxX, float maxY, float maxZ,
			bool useLocalOrientations,
			int atIndex,
			IntPtr name,
			float minConfidence,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);
		/**
		 * \brief Creates a user defined joint orientation recognizer
		 * 
		 * @param joint the joint of interest
		 * @param orientation indicates the wanted joint orientation
		 * @param maxAngleDifference (=45°) the maximum angle difference that is allowed between the requested orientation and the actual orientation
		 * @param useLocalOrientations if true, uses a local orienation in which the parent orientation has been substracted
		 * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		 * @param name (= 0) sets a name for the recognizer (should be unique!)
		 * @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		 * @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		 *
		 * @return index of the recognizer needed to call it later
		 */
		[DllImport(DllName, EntryPoint = "?addJointOrientationRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@MMMM_NHPBDM1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 addJointOrientationRecognizer(FubiUtils.SkeletonJoint joint,
			float orientX, float orientY, float orientZ,
			float maxAngleDiff,
			bool useLocalOrientations,
			int atIndex,
			IntPtr name,
			float minConfidence,
			[MarshalAs(UnmanagedType.U1)] bool useFilteredData = false);

		/**
		 * \brief load a combination recognizer from a string that represents an xml node with the combination definition
		 * 
		 * @para xmlDefinition string containing the xml definition
		 * @return true if the combination was loaded succesfully
		 */ 
		[DllImport(DllName, EntryPoint = "?addCombinationRecognizer@Fubi@@YA_NPBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool addCombinationRecognizer(IntPtr xmlDefinition);
		

		/**
		 * \brief  Whether the user is currently seen in the depth image
		 *
		 * @param userID OpenNI id of the user
		 */
		[DllImport(DllName, EntryPoint = "?isUserInScene@Fubi@@YA_NI@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool isUserInScene(UInt32 userID);

		/**
		 * \brief Whether the user is currently tracked
		 *
		 * @param userID OpenNI id of the user
		 */
		[DllImport(DllName, EntryPoint = "?isUserTracked@Fubi@@YA_NI@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool isUserTracked(UInt32 userID);

		/**
		 * \brief Get the most current tracking info of the user
		 * (including all joint positions and orientations, the center of mass and a timestamp)
		 *
		 * @param userID OpenNI id of the user
		 * @param filteredData if true the returned data will be data smoothed by the filter configured in the sensor
		 * @return the user tracking info struct
		 */
		[DllImport(DllName, EntryPoint = "?getCurrentTrackingData@Fubi@@YAPBUTrackingData@1@I_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getCurrentTrackingData(UInt32 userID, bool filteredData = false);

		/**
		 * \brief Get the last tracking info of the user (one frame before the current one)
		 * (including all joint positions and orientations, the center of mass and a timestamp)
		 *
		 * @param userID OpenNI id of the user
		 * @param filteredData if true the returned data will be data smoothed by the filter configured in the sensor
		 * @return the user tracking info struct
		 */
		[DllImport(DllName, EntryPoint = "?getLastTrackingData@Fubi@@YAPBUTrackingData@1@I_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getLastTrackingData(UInt32 userID, bool filteredData = false);

		/**
		 * \brief  Get the skeleton joint position out of the tracking info
		 *
		 * @param trackingData the trackingData struct to extract the info from
		 * @param jointID
		 * @param x, y, z the position of the joint
		 * @param confidence the confidence for this position
		 * @param timestamp (seconds since program start)
		 * @param localPosition if set to true, the function will return local position (vector from parent joint)
		 */
		[DllImport(DllName, EntryPoint = "?getSkeletonJointPosition@Fubi@@YAXPBUTrackingData@1@W4Joint@SkeletonJoint@1@AAM222AAN_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getSkeletonJointPosition(IntPtr trackingData, FubiUtils.SkeletonJoint joint, out float x, out float y, out float z,
			out float confidence, out double timeStamp, [MarshalAs(UnmanagedType.U1)] bool useLocalPositions = false);

		/**
		 * \brief  Get the skeleton joint orientation out of the tracking info
		 *
		 * @param trackingData the trackingData struct to extract the info from
		 * @param jointID
		 * @param[out] mat rotation 3x3 matrix (9 floats)
		 * @param[out] confidence the confidence for this position
		 * @param[out] timestamp (seconds since program start)
		 * @param localOrientation if set to true, the function will local orientations (cleared of parent orientation) instead of globals
		 */
		[DllImport(DllName, EntryPoint = "?getSkeletonJointOrientation@Fubi@@YAXPBUTrackingData@1@W4Joint@SkeletonJoint@1@PAMAAMAAN_N@Zc", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getSkeletonJointOrientation(IntPtr trackingData, FubiUtils.SkeletonJoint joint, [MarshalAs(UnmanagedType.LPArray)] float[] mat, 
			out float confidence, out double timeStamp, [MarshalAs(UnmanagedType.U1)] bool useLocalOrientations = true);

		/**
		* \brief  Get a users current value of a specific body measurement distance
		*
		* @param userId id of the user
		* @param measure the requested measurement
		* @param[out] dist the actual distance value
		* @param[out] confidence the confidence for this position
		*/
		[DllImport(DllName, EntryPoint = "?getBodyMeasurementDistance@Fubi@@YAXIW4Measurement@BodyMeasurement@1@AAM1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getBodyMeasurementDistance(uint userId, FubiUtils.BodyMeasurement measure, out float dist, out float confidence);

		/**
		 * \brief  Creates an empty vector of UsertrackingData structs
		 *
		 */
		[DllImport(DllName, EntryPoint = "?createTrackingDataVector@Fubi@@YAPAV?$vector@UUserTrackingData@Fubi@@V?$allocator@UUserTrackingData@Fubi@@@std@@@std@@XZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr createTrackingDataVector();
	
		/**
		 * \brief  Releases the formerly created vector
		 *
		 * @param vec the vector that will be released
		 */
		[DllImport(DllName, EntryPoint = "?releaseTrackingDataVector@Fubi@@YAXPAV?$vector@UUserTrackingData@Fubi@@V?$allocator@UUserTrackingData@Fubi@@@std@@@std@@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void releaseTrackingDataVector(IntPtr vec);

		/**
		 * \brief  Returns the size of the vector
		 *
		 * @param vec the vector that we get the size of
		 */
		[DllImport(DllName, EntryPoint = "?getTrackingDataVectorSize@Fubi@@YAIPAV?$vector@UUserTrackingData@Fubi@@V?$allocator@UUserTrackingData@Fubi@@@std@@@std@@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 getTrackingDataVectorSize(IntPtr vec);

		/**
		 * \brief  Returns one element of the tracking info vector
		 *
		 * @param vec the vector that we get the element of
		 */
		[DllImport(DllName, EntryPoint = "?getTrackingData@Fubi@@YAPAUUserTrackingData@1@PAV?$vector@UUserTrackingData@Fubi@@V?$allocator@UUserTrackingData@Fubi@@@std@@@std@@I@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getTrackingData(IntPtr vec, UInt32 index);

		/**
		 * \brief Returns the OpenNI id of the users standing closest to the sensor
		 */
		[DllImport(DllName, EntryPoint = "?getClosestUserID@Fubi@@YAIXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt32 getClosestUserID();

		/**
		 * \brief Stops and removes all user defined recognizers
		 */
		[DllImport(DllName, EntryPoint = "?clearUserDefinedRecognizers@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void clearUserDefinedRecognizers();

		/**
		 * \brief Returns all current users with their tracking information
		 * 
		 * @param pUserContainer (=0) pointer where a pointer to the current users will be stored at
		 *        The maximal size is Fubi::MaxUsers, but the current size can be requested by leaving the Pointer at 0
		 * @return the current number of users (= valid users in the container)
		 */
		[DllImport(DllName, EntryPoint = "?getCurrentUsers@Fubi@@YAGPAPAPAVFubiUser@@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern UInt16 getCurrentUsers(IntPtr container);


		/**
		 * \brief resests the tracking of all users
		 */
		[DllImport(DllName, EntryPoint = "?resetTracking@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void resetTracking();

		/**
		 * \brief get time since program start in seconds
		 */
		[DllImport(DllName, EntryPoint = "?getCurrentTime@Fubi@@YANXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern double getCurrentTime();

		/**
		 * \brief set the filtering options for smoothing the skeleton according to the 1 Euro filter (still possible to get the unfiltered data)
		 *
		 * @param minCutOffFrequency (=1.0f) the minimum cutoff frequency for low pass filtering (=cut off frequency for a still joint)
		 * @param velocityCutOffFrequency (=1.0f) the cutoff frequency for low pass filtering the velocity
		 * @param cutOffSlope (=0.007f) how fast a higher velocity will higher the cut off frequency (->apply less smoothing with higher velocities)
		 */
		[DllImport(DllName, EntryPoint = "?setFilterOptions@Fubi@@YAXMMM@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void setFilterOptions(float minCutOffFrequency = 1.0f, float velocityCutOffFrequency = 1.0f, float cutOffSlope = 0.007f);

		/**
		 * \brief get the filtering options for smoothing the skeleton according to the 1 Euro filter (still possible to get the unfiltered data)
		 *
		 * @param minCutOffFrequency the minimum cutoff frequency for low pass filtering (=cut off frequency for a still joint)
		 * @param velocityCutOffFrequency the cutoff frequency for low pass filtering the velocity
		 * @param cutOffSlope how fast a higher velocity will higher the cut off frequency (->apply less smoothing with higher velocities)
		 */
		[DllImport(DllName, EntryPoint = "?getFilterOptions@Fubi@@YAXAAM00@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getFilterOptions(out float minCutOffFrequency, out float velocityCutOffFrequency, out float cutOffSlope);

		/**
		 * \brief Returns the ids of all users order by their distance to the sensor (x-z plane)
		 * Closest user is at the front, user with largest distance or untracked users at the back
		 * @param userIDs an array big enough to receive the indicated number of user ids (Fubi::MaxUsers at max)
		 * @param maxNumUsers if greater than -1, the given number of closest users is additionally ordered from left to right position
		 * @return the actual number of user ids written into the array
		 */
		[DllImport(DllName, EntryPoint = "?getClosestUserIDs@Fubi@@YAIPAIH@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern uint getClosestUserIDs(uint[] userIDs, int maxNumUsers = -1);

		/**
		 * \brief Set the current tracking info of one user
		 * (including all joint positions. Optionally the orientations and a timestamp)
		 *
		 * @param userID id of the user
		 * @param skeleton i.e. NUM_JOINTS * (position+orientation) with position, orientation all as 4 floats (x,y,z,conf) in milimeters or degrees
		 * @param timestamp the timestamp of the tracking value (if -1 an own timestamp will be created)#
		 */
		[DllImport(DllName, EntryPoint = "?updateTrackingData@Fubi@@YAXIPAMN@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void updateTrackingData(uint userID, IntPtr skeleton, double timeStamp);

		/**
		 * \brief Creates a user defined angular movement recognizer
		 * 
		 * @param joint the joint of interest
		 * @param minVelX/Y/Z the minimum angular velocity per axis (also defines the rotation direction)
		 * @param maxVelX/Y/Z the maximum angular velocity per axis (also defines the rotation direction)
		 * @param useLocalrOrients whether local ("substracted" parent orientation = the actual joint orientation, not the orientation in space)
		 *		  or global orientations should be used
		 * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
		 * @param name name of the recognizer
		 * @param minConfidence (=-1) if given this is the mimimum confidence required from tracking for the recognition to be succesful
		 * @param useFilteredData (=false) if true, the recognizer will use the filtered tracking data instead of the raw one
		 *
		 * @return index of the recognizer needed to call it later
		 */
		[DllImport(DllName, EntryPoint = "?addAngularMovementRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@MMMMMM_NHPBDM1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern uint addAngularMovementRecognizer(FubiUtils.SkeletonJoint joint, 
			float minVelX, float minVelY, float minVel,
			float maxVelX, float maxVelY, float maxVelZ,
			bool useLocalOrients,
			int atIndex, IntPtr name,
			float minConfidence, [MarshalAs(UnmanagedType.U1)] bool useFilteredData);

		/**
		* \brief Creates a user defined dynamic time warping recognizer
		*
		* @param joints the joint of interest
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
		[DllImport(DllName, EntryPoint = "?addTemplateRecognizer@Fubi@@YAIW4Joint@SkeletonJoint@1@0PBDHHMW4Measure@DistanceMeasure@1@M_NI333W4Model@StochasticModel@1@IW4Measurement@BodyMeasurement@1@H1M33@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal static extern uint addTemplateRecognizer(FubiUtils.SkeletonJoint joint, FubiUtils.SkeletonJoint relJoint,
		    IntPtr trainingDataFile, int startFrame, int endFrame,
		    float maxDistance,
		    FubiUtils.DistanceMeasure distanceMeasure,
		    float maxRotation,
            [MarshalAs(UnmanagedType.U1)]bool aspectInvarian,
		    uint ignoreAxes,
            [MarshalAs(UnmanagedType.U1)]bool useOrientation,
			[MarshalAs(UnmanagedType.U1)]bool useDTW,
			[MarshalAs(UnmanagedType.U1)]bool applyResampling,
            FubiUtils.StochasticModel stochasticModel,
            uint numGMRStates,
		    FubiUtils.BodyMeasurement measuringUnit,
		    int atIndex, IntPtr name,
		    float minConfidence,
            [MarshalAs(UnmanagedType.U1)]bool useLocalTransformations,
            [MarshalAs(UnmanagedType.U1)]bool useFilteredData);

	    /**
	    * \brief Creates a user defined dynamic time warping recognizer
	    *
	    * @param xmlDefinition string containing the xml definition of the recognizer
	    * @param atIndex (= -1) if an index is given, the corresponding recognizer will be replaced instead of creating a new one
	    *
	    * @return index of the recognizer needed to call it later
	    */
		[DllImport(DllName, EntryPoint = "?addTemplateRecognizer@Fubi@@YAIPBDH@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        internal static extern uint addTemplateRecognizer(IntPtr xmlDefinition, int atIndex);

		/**
		 * \brief Checks a user defined combination recognizer for its current state
		 * 
		 * @param recongizerName name of the combination
		 * @param userID the OpenNI user id of the user to be checked
		 * @param numStates (out) the full number of states of this recognizer
		 * @param isInterrupted (out) whether the recognizers of the current state are temporarly interrupted
		 * @param isInTransition (out) if the state has already passed its min duration and would be ready to transit to the next state
		 * @return number of current state (0..numStates-1), if < 0 -> error: -1 if first state not yet started, -2 user not found, -3 recognizer not found
		 */
		[DllImport(DllName, EntryPoint = "?getCurrentCombinationRecognitionState@Fubi@@YAHPBDIAAIAA_N2@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern int getCurrentCombinationRecognitionState(IntPtr recognizerName, uint userID, out uint numStates, [MarshalAs(UnmanagedType.U1)] out bool isInterrupted, [MarshalAs(UnmanagedType.U1)] out bool isInTransition);

		/**
		 * \brief Get meta information of a state of one recognizers
		 * 
		 * @param recognizerName name of the combination
		 * @param stateIndex the state index to get the meta info from
		 * param propertyName the name of the property to get
		 * @return the value of the requested meta info property as a string, or 0x0 on error
		 */
		[DllImport(DllName, EntryPoint = "?getCombinationRecognitionStateMetaInfo@Fubi@@YAPBDPBDI0@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getCombinationRecognitionStateMetaInfo(IntPtr recognizerName, uint stateIndex, IntPtr propertyName);

		/**
		 * \brief initalizes a finger sensor such as the leap motion for tracking fingers
		 * 
		 * @param type the sensor type (see FingerSensorType definition)
		 * @return true if successful initialized
		 */
		[DllImport(DllName, EntryPoint = "?initFingerSensor@Fubi@@YA_NW4Type@FingerSensorType@1@MMM@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool initFingerSensor(FubiUtils.FingerSensorType type, float offsetPosX, float offsetPosY, float offsetPosZ);

		/**
		 * \brief Get the currently available finger sensor types (defined in FubiConfig.h before compilation)
		 * 
		 * @return an int composed of the currently available sensor types (see FingerSensorType enum for the meaning)
		 */
		[DllImport(DllName, EntryPoint = "?getAvailableFingerSensorTypes@Fubi@@YAHXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern int getAvailableFingerSensorTypes();

		/**
		 * \brief Get the type of the currently active sensor
		 * 
		 * @return the current sensor type
		 */
		[DllImport(DllName, EntryPoint = "?getCurrentFingerSensorType@Fubi@@YA?AW4Type@FingerSensorType@1@XZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.FingerSensorType getCurrentFingerSensorType();

		/**
		 * \brief Returns the number of currently tracked hands
		 * 
		 * @return the current number of hands
		 */
		[DllImport(DllName, EntryPoint = "?getNumHands@Fubi@@YAGXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern ushort getNumHands();

		/**
		 * \brief Returns the hand id from the user index
		 * 
		 * @param index index of the hand in the hand array
		 * @return hand id of that user or 0 if not found
		 */
		 [DllImport(DllName, EntryPoint = "?getHandID@Fubi@@YAII@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern uint getHandID(uint index);

		/**
		* \brief Get the most current tracking info of a hand
		* (including all joint positions and orientations (local and global) and a timestamp)
		*
		* @param handId id of the hand
		* @param filteredData if true the returned data will be data smoothed by the filter configured in the sensor
		* @return the tracking info struct
		*/
		[DllImport(DllName, EntryPoint = "?getCurrentFingerTrackingData@Fubi@@YAPBUTrackingData@1@I_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern IntPtr getCurrentFingerTrackingData(uint handId, bool filteredData);

		/**
		* \brief  Get the skeleton joint position out of the tracking info
		*
		* @param trackingInfo the trackinginfo struct to extract the info from
		* @param joint the considered joint id
		* @param[out] x, y, z where the position of the joint will be copied to
		* @param[out] confidence where the confidence for this position will be copied to
		* @param[out] timeStamp where the timestamp of this tracking info will be copied to (seconds since program start)
		* @param localPosition if set to true, the function will return local position (vector from parent joint)
		*/
		[DllImport(DllName, EntryPoint = "?getSkeletonHandJointPosition@Fubi@@YAXPBUTrackingData@1@W4Joint@SkeletonHandJoint@1@AAM222AAN_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getSkeletonHandJointPosition(IntPtr trackingData, FubiUtils.SkeletonHandJoint joint,
			out float x, out float y, out float z, out float confidence, out double timeStamp, bool localPosition);

		/**
		* \brief  Get the skeleton joint orientation out of the tracking info
		*
		* @param trackingInfo the trackinginfo struct to extract the info from
		* @param joint the considered joint id
		* @param[out] mat rotation 3x3 matrix (9 floats)
		* @param[out] confidence the confidence for this position
		* @param[out] timeStamp (seconds since program start)
		* @param localOrientation if set to true, the function will local orientations (cleared of parent orientation) instead of globals
		*/
		[DllImport(DllName, EntryPoint = "?getSkeletonHandJointOrientation@Fubi@@YAXPBUTrackingData@1@W4Joint@SkeletonHandJoint@1@PAMAAMAAN_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getSkeletonHandJointOrientation(IntPtr trackingData, FubiUtils.SkeletonHandJoint joint,
			float[] mat, out float confidence, out double timeStamp, bool localOrientation);

		/**
		* \brief Returns the hand id of the closest hand
		*
		* @return hand id of that hand or 0 if none found
		*/
		[DllImport(DllName, EntryPoint = "?getClosestHandID@Fubi@@YAIXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern uint getClosestHandID();

		/**
		 * \brief Checks a user defined gesture or posture recognizer for its success
		 * 
		 * @param recognizerName name of the recognizer return during its creation
		 * @param userID of the hand to be checked
		 * @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		 */
		[DllImport(DllName, EntryPoint = "?recognizeGestureOnHand@Fubi@@YA?AW4Result@RecognitionResult@1@PBDI@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult recognizeGestureOnHand(IntPtr recognizerName, uint handID);

		/**
		 * \brief Checks a user defined combination recognizer for its progress
		 * 
		 * @param recongizerName name of the combination
		 * @param handID of the hand to be checked
		 * @param handStates (= 0x0) pointer to a vector of tracking data that represents the tracking information of the user
		 *		  during the recognition of each state
		 * @param restart (=true) if set to true, the recognizer automatically restarts, so the combination can be recognized again.
		 * @param returnFilteredData if true, the user states vector will contain filtered data
		 * @param correctionHint on NOT_RECOGNIZED, this struct will contain information about why the recognition failed if wanted
		 * @return RECOGNIZED in case of a succesful detection, TRACKING_ERROR if a needed joint is currently not tracked, NOT_RECOGNIZED else
		 */
		[DllImport(DllName, EntryPoint = "?getCombinationRecognitionProgressOnHand@Fubi@@YA?AW4Result@RecognitionResult@1@PBDIPAV?$vector@UTrackingData@Fubi@@V?$allocator@UTrackingData@Fubi@@@std@@@std@@_N2PAURecognitionCorrectionHint@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognitionResult getCombinationRecognitionProgressOnHand(IntPtr recognizerName, uint handID,
			IntPtr handStates, [MarshalAs(UnmanagedType.U1)]bool restart = true, [MarshalAs(UnmanagedType.U1)]bool returnFilteredData = false, [In, Out] FubiUtils.RecognitionCorrectionHint hint = null);

		/**
		 * \brief Starts or stops the recognition process of a user defined combination for one hand
		 * 
		 * @param combinationName name defined for this recognizer
		 * @param handID of the hand for which the recognizers should be modified
		 * @param enable if set to true, the recognizer will be started (if not already stared), else it stops
		 */
		[DllImport(DllName, EntryPoint = "?enableCombinationRecognitionHand@Fubi@@YAXPBDI_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void enableCombinationRecognitionHand(IntPtr combinationName, uint handID, [MarshalAs(UnmanagedType.U1)]bool enable);

		/**
		 * \brief Checks a user defined combination recognizer for its current state
		 * 
		 * @param recognizerName name of the combination
		 * @param handID of the hand to be checked
		 * @param numStates (out) the full number of states of this recognizer
		 * @param isInterrupted (out) whether the recognizers of the current state are temporarly interrupted
		 * @param isInTransition (out) if the state has already passed its min duration and would be ready to transit to the next state
		 * @return number of current state (0..numStates-1), if < 0 -> error: -1 if first state not yet started, -2 user not found, -3 recognizer not found
		 */
		[DllImport(DllName, EntryPoint = "?getCurrentCombinationRecognitionStateForHand@Fubi@@YAHPBDIAAIAA_N2@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern int getCurrentCombinationRecognitionStateForHand(IntPtr recognizerName, uint handID, out uint numStates, [MarshalAs(UnmanagedType.U1)]out bool isInterrupted, [MarshalAs(UnmanagedType.U1)]out bool isInTransition);

		/**
		 * \brief Get the offset position of the current finger sensor to the main sensor
		 * 
		 * @param Offset (out) a vector from the main sensor to the finger sensor, (0,0,0) if no sensor present
		 */
		[DllImport(DllName, EntryPoint = "?getFingerSensorOffsetPosition@Fubi@@YAXAAM00@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getFingerSensorOffsetPosition(out float xOffset, out float yOffset, out float zOffset);
	
		/**
		 * \brief Set the offset position of the current finger sensor to the main sensor
		 * 
		 * @param OffsetPos the vector from the main sensor to the finger sensor
		 */
		[DllImport(DllName, EntryPoint = "?setFingerSensorOffsetPosition@Fubi@@YAXMMM@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void setFingerSensorOffsetPosition(float xOffset, float yOffset, float zOffset);

		/**
		 * \brief Get the target sensor of a user defined combination recognizer
		 * 
		 * @param recognizerName name of the combination
		 * @return the target sensor as defined in FubiUtils.h
		 */
		[DllImport(DllName, EntryPoint = "?getCombinationRecognizerTargetSkeleton@Fubi@@YA?AW4Target@RecognizerTarget@1@PBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognizerTarget getCombinationRecognizerTargetSkeleton(IntPtr recognizerName);

		/**
		 * \brief Get the target sensor for a recognizer
		 * 
		 * @param recognizerName name of the recognizer
		 * @return the target sensor as defined in FubiUtils.h
		 */
		[DllImport(DllName, EntryPoint = "?getRecognizerTargetSkeleton@Fubi@@YA?AW4Target@RecognizerTarget@1@PBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern FubiUtils.RecognizerTarget getRecognizerTargetSkeleton(IntPtr recognizerName);

		/**
		* \brief Convert coordinates between real world, depth, color, or IR image
		*
		* @param inputCoordsX, inputCoordsY, inputCoordsZ the coordiantes to convert
		* @param[out] outputCoordsX, outputCoordsY, outputCoordsZ vars to store the converted coordinates to
		* @param inputType the type of the inputCoords
		* @param outputType the type to convert the inputCoords to
		*/
		[DllImport(DllName, EntryPoint = "?convertCoordinates@Fubi@@YAXMMMAAM00W4Type@CoordinateType@1@1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void convertCoordinates(float inputCoordsX, float inputCoordsY, float inputCoordsZ, out float outputCoordsX, out float outputCoordsY, out float outputCoordsZ,
		FubiUtils.CoordinateType inputType, FubiUtils.CoordinateType outputType);

		/**
		* \brief Get the floor plane provided by some sensors in the Hesse normal form.
		*		 If not supported by the sensor, the normal will have length 0, else 1.
		* @param[out] normalX, normalY, normalZ the unit normal vector that points from the origin of the coordinate system to the plane
		* @param[out] dist the distance from the origin to the plane
		*/
		[DllImport(DllName, EntryPoint = "?getFloorPlane@Fubi@@YAXAAM000@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getFloorPlane(out float normalX, out float normalY, out float normalZ, out float dist);

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
		[DllImport(DllName, EntryPoint = "?startRecordingSkeletonData@Fubi@@YA_NPBDI_N1@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool startRecordingSkeletonData(IntPtr fileName, uint targetID, [MarshalAs(UnmanagedType.U1)]bool useFilteredData, [MarshalAs(UnmanagedType.U1)]bool isHand);

		/**
		* \brief Check whether a recording process is currently running
		* 
		* @param[out] currentFrameID last frame id that has already been feed into Fubi
		* @return whether a recording process is currently running
		*/
		[DllImport(DllName, EntryPoint = "?isRecordingSkeletonData@Fubi@@YA_NPAH@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool isRecordingSkeletonData(ref int currentFrameID);

		/**
		* \brief Stop previously started recording of tracking data and close the corresponding file
		*/
		[DllImport(DllName, EntryPoint = "?stopRecordingSkeletonData@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void stopRecordingSkeletonData();

		/**
		* \brief Load tracking data from a previously recorded file for later playback
		*
		* @param fileName name of the file to play back
		* @return number of loaded frames, -1 if something went wrong
		*/
		[DllImport(DllName, EntryPoint = "?loadRecordedSkeletonData@Fubi@@YAHPBD@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern int loadRecordedSkeletonData(IntPtr fileName);

		/**
		* \brief Set the playback markers of a previously loaded recording
		*
		* @param currentFrame the current playback marker
		* @param startFrame the first frame to be played
		* @param endFrame (=-1) the last frame to be played (-1 for the end of file)
		*/
		[DllImport(DllName, EntryPoint = "?setPlaybackMarkers@Fubi@@YAXHHH@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void setPlaybackMarkers(int currentFrame, int startFrame, int endFrame);

		/**
		* \brief Get the current playback markers of a previously loaded recording
		*
		* @param[out] currentFrame the current playback marker
		* @param[out] startFrame the first frame to be played
		* @param[out] endFrame the last frame to be played
		*/
		[DllImport(DllName, EntryPoint = "?getPlaybackMarkers@Fubi@@YAXAAH00@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getPlaybackMarkers(ref int currentFrame, ref int startFrame, ref int endFrame);

		/**
		* \brief Start play back of a previously loaded recording
		*
		* @param loop (=false) if true, playback restarts when the endframe is reached
		*/
		[DllImport(DllName, EntryPoint = "?startPlayingSkeletonData@Fubi@@YAX_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void startPlayingSkeletonData([MarshalAs(UnmanagedType.U1)]bool loop);


		/**
		* \brief Check whether the play back of a recording is still running
		*
		* @param[out] currentFrameID last frame id that has already been feed into Fubi
		* @param[out] isPaused whether the playback is paused at the current frame
		* @return whether the play back of a recording is still running
		*/
		[DllImport(DllName, EntryPoint = "?isPlayingSkeletonData@Fubi@@YA_NPAHPA_N@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool isPlayingSkeletonData(ref int currentFrameID, [MarshalAs(UnmanagedType.U1)]ref bool isPaused);

		/**
		* \brief Stop previously started playing of tracking data
		*/
		[DllImport(DllName, EntryPoint = "?stopPlayingRecordedSkeletonData@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void stopPlayingRecordedSkeletonData();

		/**
		* \brief Pause previously started playing of tracking data (keeps the current user in the scene)
		*/
		[DllImport(DllName, EntryPoint = "?pausePlayingRecordedSkeletonData@Fubi@@YAXXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void pausePlayingRecordedSkeletonData();

		/**
		* \brief Get the playback duration for the currently set start and end markers
		*
		* @return playback duration in seconds
		*/
		[DllImport(DllName, EntryPoint = "?getPlaybackDuration@Fubi@@YANXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern double getPlaybackDuration();

		/**
		* \brief Trim the previously loaded playback file to the currently set markers
		*
		* @return true if successful
		*/
		[DllImport(DllName, EntryPoint = "?trimPlaybackFileToMarkers@Fubi@@YA_NXZ", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool trimPlaybackFileToMarkers();

		/**
		* \brief delegate type which can be passed to the C++ API as function pointer
		*/
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate void RecognitionCallbackDelegate(IntPtr gestureName, uint targetID, [MarshalAs(UnmanagedType.U1)]bool isHand, FubiUtils.RecognizerType recognizerType);

		/**
		* \brief set a call back function which will be called for every gesture recognition that occurs
		*  You can set a null pointer if you don't want to get callbacks any more
		*  The callback function will be called inside of the updateSensor() call
		*  GetCombinationProgress() conflicts with the callbacks and won't work anymore
		*
		* @param startCallback function that should be called at the start of recognition events; paramters: const char* gestureName, usigned int targetID, bool isHand, RecognizerType::Type recognizerType
		* @param endCallback function that should be called at the end of recognition events; paramters: const char* gestureName, usigned int targetID, bool isHand, :RecognizerType::Type recognizerType
		*/
		[DllImport(DllName, EntryPoint = "?setRecognitionCallbacks@Fubi@@YAXP6AXPBDI_NW4Type@RecognizerType@1@@Z3@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void setRecognitionCallbacks(RecognitionCallbackDelegate startCallback, RecognitionCallbackDelegate endCallback);

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
		[DllImport(DllName, EntryPoint = "?getImage@Fubi@@YA_NPAEIW4Channel@ImageNumChannels@1@W4Depth@ImageDepth@1@@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool getImage(IntPtr outputImage, uint imageIndex, FubiUtils.ImageNumChannels numChannels, FubiUtils.ImageDepth depth);

		/**
		* \brief get the image width, height and number of streams of the current finger sensor
		*        assumes that all streams have the same width and height
		*
		* @param[out] width image width
		* @param[out] height image width
		* @param[out] numStreams number of image streams provided by the current finger sensor
		*/
		[DllImport(DllName, EntryPoint = "?getFingerSensorImageConfig@Fubi@@YAXAAH0AAI@Z", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		internal static extern void getFingerSensorImageConfig(out int width, out int height, out uint numStreams);
	};
}
