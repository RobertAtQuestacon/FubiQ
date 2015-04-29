#include "Fubi.h"

#include <iostream>
#include <string>
#include <sstream>
#include <queue>

#ifdef __APPLE__
#include <glut.h>
#else
#include <GL/glut.h>
#endif

#include <FubiUtils.h>

#if defined ( WIN32 ) || defined( _WINDOWS )
#include <Windows.h>
#endif

//#include <vld.h>

using namespace Fubi;

// Some additional OpenGL defines
#define GL_GENERATE_MIPMAP_SGIS           0x8191
#define GL_GENERATE_MIPMAP_HINT_SGIS      0x8192
#define GL_BGRA                           0x80E1

// Configure in which format Fubi and OpenGL should operate the image streams (need to fit together)
#define GL_DATA_FORMAT	GL_UNSIGNED_BYTE			// U_SHORT,	U_BYTE,	U_FLOAT
#define FUBI_FORMAT		Fubi::ImageDepth::D8		// D16,		D8,		F32
#define NUM_CHANNELS		Fubi::ImageNumChannels::C4	// C3,		C4
#define GL_CHANNEL_FORMAT	GL_BGRA						// GL_RGB,	GL_BGRA
#define BYTES_PER_CHANNEL	(FUBI_FORMAT/8)
#define COLOR_STREAM_OPTIONS	StreamOptions()
#define IR_STREAM_OPTIONS		StreamOptions(-1)

// Some global variables for the application
int g_window = 0;
unsigned char* g_depthData = 0x0;
unsigned char* g_rgbData = 0x0;
unsigned char* g_irData = 0x0;

int dWidth = 0, dHeight = 0, rgbWidth = 0, rgbHeight = 0, irWidth = 0, irHeight = 0;

Fubi::ImageType::Type g_imageType = Fubi::ImageType::Depth;

short g_showInfo = 0;

bool g_showFingerCounts = false;
bool g_useOldFingerCounts = false;

RecognizerType::Type g_recognizerToCheck = RecognizerType::USERDEFINED_GESTURE;
bool g_takePictures = false;
bool g_exitNextFrame = false;

double g_recordingCountDown = -1.0;
const char* g_fileNameForRecording = "trainingData/tempRecord.xml";

void recognitionStartCallback(const char* recName, unsigned int targetId, bool isHand, RecognizerType::Type type)
{
    if (type == g_recognizerToCheck)
        printf("%s %u STARTED rec %s -->\n", isHand ? "Hand" : "User", targetId, recName);
}

void recognitionEndCallback(const char* recName, unsigned int targetId, bool isHand, RecognizerType::Type type)
{
    if (type == g_recognizerToCheck)
        printf("--> %s %u ENDED rec %s\n", isHand ? "Hand" : "User", targetId, recName);
}

void glutIdle(void)
{
    // Display the frame
    glutPostRedisplay();
}

// The glut update functions called every frame
void glutDisplay(void)
{
    if (g_exitNextFrame)
    {
        // Release the window
        glutDestroyWindow(g_window);
        // Release Fubi
        release();
        // And all allocated buffers
        delete[] g_depthData;
        delete[] g_rgbData;
        delete[] g_irData;
        exit(0);
    }

    // Update the sensor
    updateSensor();

    ImageType::Type type = g_imageType;
    unsigned char* buffer = g_depthData;
    if (Fubi::getCurrentSensorType() == SensorType::NONE)
    {
        type = ImageType::Blank;
        memset(buffer, 0, dWidth*dHeight*NUM_CHANNELS*BYTES_PER_CHANNEL);
    }
    else if (type == ImageType::Color)
    {
        buffer = g_rgbData;
    }
    else if (type == ImageType::IR)
    {
        buffer = g_irData;
    }

    unsigned int options = RenderOptions::None;
    DepthImageModification::Modification mod = DepthImageModification::UseHistogram;
    if (g_showInfo == 0)
        options = RenderOptions::Shapes | RenderOptions::UserCaptions | RenderOptions::Skeletons | RenderOptions::FingerShapes | RenderOptions::Background | RenderOptions::DetailedFaceShapes | RenderOptions::UseFilteredValues | RenderOptions::FingerShapes;
    else if (g_showInfo == 1)
        options = RenderOptions::Shapes | RenderOptions::LocalOrientCaptions | RenderOptions::Skeletons;
    else if (g_showInfo == 2)
        mod = DepthImageModification::ConvertToRGB;
    else if (g_showInfo == 3)
        mod = DepthImageModification::StretchValueRange;

    if (GL_CHANNEL_FORMAT == GL_RGB && type != ImageType::Color)
        options |= RenderOptions::SwapRAndB;

    getImage(buffer, type, NUM_CHANNELS, FUBI_FORMAT, options, RenderOptions::ALL_JOINTS, mod/*, getClosestUserID(), SkeletonJoint::HEAD, true*/);

    // Clear the OpenGL buffers
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

    // Setup the OpenGL viewpoint
    glMatrixMode(GL_PROJECTION);
    glPushMatrix();
    glLoadIdentity();
    glOrtho(0, (double)dWidth, (double)dHeight, 0, -1.0, 1.0);

    // Create the OpenGL texture map
    glEnable(GL_TEXTURE_2D);
    glTexParameteri(GL_TEXTURE_2D, GL_GENERATE_MIPMAP_SGIS, GL_TRUE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    if (type == ImageType::Color)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, rgbWidth, rgbHeight, 0, (GL_CHANNEL_FORMAT == GL_BGRA) ? GL_RGBA : GL_RGB, GL_DATA_FORMAT, g_rgbData);
    else if (type == ImageType::IR)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, irWidth, irHeight, 0, GL_CHANNEL_FORMAT, GL_DATA_FORMAT, g_irData);
    else
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, dWidth, dHeight, 0, GL_CHANNEL_FORMAT, GL_DATA_FORMAT, g_depthData);

    // Display the OpenGL texture map
    glColor4f(1, 1, 1, 1);
    glBegin(GL_QUADS);
    // upper left
    glTexCoord2f(0, 0);
    glVertex2f(0, 0);
    // upper right
    glTexCoord2f(1.0f, 0);
    glVertex2f((float)dWidth, 0);
    // bottom right
    glTexCoord2f(1.0f, 1.0f);
    glVertex2f((float)dWidth, (float)dHeight);
    // bottom left
    glTexCoord2f(0, 1.0f);
    glVertex2f(0, (float)dHeight);
    glEnd();
    glDisable(GL_TEXTURE_2D);

    unsigned int closestID = getClosestUserID();
    bool isHand = false;
    if (closestID == 0)
    {
        isHand = true;
        closestID = getClosestHandID();
    }

    if (g_recordingCountDown > 0)
    {
        if (!isHand)
            enableFingerTracking(closestID, true, true, g_useOldFingerCounts);
        if (Fubi::getCurrentTime() >= g_recordingCountDown)
        {
            if (startRecordingSkeletonData(g_fileNameForRecording, closestID, false, isHand))
            {
                printf("\nStarted Recording to %s\n", g_fileNameForRecording);
            }
            else
            {
                printf("\nUnable to start recording to %s!\n", g_fileNameForRecording);
            }
            g_recordingCountDown = -1.0f;
        }
        else
            printf("\rRecording in %.2f seconds", g_recordingCountDown - Fubi::getCurrentTime());
    }
    else
    {
        if (closestID > 0)
        {
            if (g_showFingerCounts)
            {
                static int pause = 0;
                if (pause == 0)
                {
                    if (isHand)
                    {
                        FubiHand* hand = getHand(closestID);
                        if (hand)
                            printf("Hand %u finger count: %d\n", closestID, ((const FingerTrackingData*)hand->currentTrackingData())->fingerCount);
                    }
                    else
                    {
                        printf("User %u finger count: left=%d, right=%d\n", closestID,
                               getFingerCount(closestID, true, true, 10, g_useOldFingerCounts),
                               getFingerCount(closestID, false, true, 10, g_useOldFingerCounts));
                    }
                }
                pause = (pause + 1) % 10;
            }
        }
    }

    /*RecognitionCorrectionHint  hint;
    recognizeGestureOn("arrow", isPlayingSkeletonData() ? PlaybackUserID : closestID, &hint);
    if (hint.m_changeType == RecognitionCorrectionHint::FORM && hint.m_dist < Math::MaxFloat)
    	printf("Dist: %.3f\n", hint.m_dist);*/

    // Swap the OpenGL display buffers
    glutSwapBuffers();

    if (g_takePictures)
    {
        static int pause = 0;
        if (pause == 0)
        {
            static int num = 0;
            std::stringstream str;
            str << "savedImages/rgbImage" << num << ".png";
            saveImage(str.str().c_str(), 95, ImageType::Color, ImageNumChannels::C3, ImageDepth::D8, RenderOptions::None,
                      -1, DepthImageModification::Raw, closestID);
            str.str("");
            str << "savedImages/depthImage" << num << ".png";
            saveImage(str.str().c_str(), 95, ImageType::Depth, ImageNumChannels::C4, ImageDepth::D8,
                      RenderOptions::Shapes | RenderOptions::Skeletons | RenderOptions::DetailedFaceShapes | RenderOptions::UseFilteredValues,
                      -1, DepthImageModification::UseHistogram, closestID);
            str.str("");
            str << "savedImages/trackingImage" << num << ".png";
            saveImage(str.str().c_str(), 95, ImageType::Depth, ImageNumChannels::C4, ImageDepth::D8,
                      RenderOptions::Skeletons | RenderOptions::DetailedFaceShapes | RenderOptions::UseFilteredValues,
                      -1, DepthImageModification::UseHistogram, closestID);
            num++;
        }
        pause = (pause + 1) % 30;
    }
}

void startNextFingerSensor()
{
    std::cout << "Trying to start the next available finger sensor type" << std::endl;
    FingerSensorType::Type type = Fubi::getCurrentFingerSensorType();
    bool success = false;
    while (!success)
    {
        if (type == FingerSensorType::NONE)
            type = FingerSensorType::LEAP;
        else if (type == FingerSensorType::LEAP)
            type = FingerSensorType::NONE;

        // init finger sensor, but you should actually set you individual offset position
        success = Fubi::initFingerSensor(type, 0, -600.0f, 200.0f);

        if (type == SensorType::NONE)
            break;	// None should always be successful so we ensure termination of this loop
    }
}

void startNextSensor()
{
    std::cout << "Trying to start the next available sensor type" << std::endl;
    SensorType::Type type = Fubi::getCurrentSensorType();
    bool success = false;
    while (!success)
    {
        if (type == SensorType::NONE)
            type = SensorType::OPENNI2;
        else if (type == SensorType::OPENNI2)
            type = SensorType::OPENNI1;
        else if (type == SensorType::OPENNI1)
            type = SensorType::KINECTSDK;
        else if (type == SensorType::KINECTSDK)
            type = SensorType::KINECTSDK2;
        else if (type == SensorType::KINECTSDK2)
            type = SensorType::NONE;

        if (Fubi::isInitialized())
            success = Fubi::switchSensor(SensorOptions(StreamOptions(), COLOR_STREAM_OPTIONS, IR_STREAM_OPTIONS, type, SkeletonTrackingProfile::ALL, true, true));
        else
            success = Fubi::init(SensorOptions(StreamOptions(), COLOR_STREAM_OPTIONS, IR_STREAM_OPTIONS, type, SkeletonTrackingProfile::ALL, true, true));

        if (success)
        {
            int w, h;
            if (type == SensorType::NONE && (dWidth != 640 || dHeight != 640))
            {
                w = 640;
                h = 480;
            }
            else
                getDepthResolution(w, h);
            if (w > 0 && h > 0 && (w != dWidth || h != dHeight))
            {
                dWidth = w;
                dHeight = h;
                getRgbResolution(rgbWidth, rgbHeight);
                getIRResolution(irWidth, irHeight);

                delete[] g_depthData;
                g_depthData = 0x0;
                delete[] g_rgbData;
                g_rgbData = 0x0;
                delete[] g_irData;
                g_irData = 0x0;
                g_depthData = new unsigned char[dWidth*dHeight*NUM_CHANNELS*BYTES_PER_CHANNEL];
                if (rgbWidth > 0 && rgbHeight > 0)
                    g_rgbData = new unsigned char[rgbWidth*rgbHeight*NUM_CHANNELS*BYTES_PER_CHANNEL];
                if (irWidth > 0 && irHeight > 0)
                    g_irData = new unsigned char[irWidth*irHeight*NUM_CHANNELS*BYTES_PER_CHANNEL];
                if (dWidth > 0 && dHeight > 0)
                {
                    glutReshapeWindow(dWidth, dHeight);
#if defined ( WIN32 ) || defined( _WINDOWS )
                    SetWindowPos(GetConsoleWindow(), HWND_TOP, dWidth + 10, 0, 0, 0,
                                 SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOZORDER);
#endif
                }
            }
        }

        if (type == SensorType::NONE)
            break;	// None should always be successful so we ensure termination of this loop
    }
}

// Glut keyboards callback
void glutKeyboard(unsigned char key, int x, int y)
{
    //printf("key: %d\n", key);
    switch (key)
    {
    case 27: //ESC
        g_exitNextFrame = true;
        break;
    case ' ':
    {
        g_recognizerToCheck = (RecognizerType::Type)((g_recognizerToCheck + 1) % (RecognizerType::NUM_TYPES+1));
        char* modeName = "Unknown";
        switch (g_recognizerToCheck)
        {
        case RecognizerType::USERDEFINED_GESTURE:
            modeName = "User defined joint relations, orientation, linear/angular movments, finger counts,  and template recs";
            break;
        case RecognizerType::USERDEFINED_COMBINATION:
            modeName = "User defined combinations";
            break;
        case RecognizerType::PREDEFINED_COMBINATION:
            modeName = "Predefined combinations";
            break;
        case RecognizerType::PREDEFINED_GESTURE:
            modeName = "Predefined gestures";
            break;
        case RecognizerType::NUM_TYPES:
            modeName = "PAUSED";
            break;
        }
        std::cout << "Check recognizer mode: " << g_recognizerToCheck << "- " << modeName << std::endl;
    }
    break;
    case 'p':
    {
        g_takePictures = !g_takePictures;
        std::cout << "Taking pictures " << (g_takePictures ? "enabled" : "disabled") << std::endl;
    }
    break;
    case 'r':
    {
        g_imageType = (g_imageType == ImageType::Color) ? ImageType::Depth : ImageType::Color;
        std::cout << "Image type changed to " << ((g_imageType == ImageType::Color) ? "color" : "depth") << std::endl;
    }
    break;
    case 'i':
    {
        g_imageType = (g_imageType == ImageType::IR) ? ImageType::Depth : ImageType::IR;
        std::cout << "Image type changed to " << ((g_imageType == ImageType::IR) ? "ir" : "depth") << std::endl;
    }
    break;
    case 'f':
    {
        g_showFingerCounts = !g_showFingerCounts;
        enableFingerTracking(getClosestUserID(), g_showFingerCounts, g_showFingerCounts, g_useOldFingerCounts);
        std::cout << "Showing finger counts " << (g_showFingerCounts ? "enabled" : "disabled") << std::endl;
    }
    break;
    case 'c':
    {
        g_useOldFingerCounts = !g_useOldFingerCounts;
        if (g_useOldFingerCounts)
            std::cout << "Using old convexity defect method for finger count calculation" << std::endl;
        else
            std::cout << "Using new morphological method for finger count calculation" << std::endl;
    }
    break;
    case 't':
    {
        g_showInfo = (g_showInfo + 1) % 4;
        std::cout << "Switching to next mode for rendering tracking info: " << g_showInfo << std::endl;
    }
    break;
    case 's':
    {
        startNextSensor();
    }
    break;
    case 'd':
        startNextFingerSensor();
        break;
    case 9: //TAB
    {
        // Reload recognizers from xml
        clearUserDefinedRecognizers();
        if (loadRecognizersFromXML("SampleRecognizers.xml"))
            std::cout << "Succesfully reloaded recognizers xml!" << std::endl;
        else
            std::cout << "Error reloading recognizers xml!" << std::endl;

    }
    break;
    case 13: //ENTER/RETURN
        if (isRecordingSkeletonData())
        {
            stopRecordingSkeletonData();
            enableFingerTracking(getClosestUserID(), false, false, g_useOldFingerCounts);
            g_recordingCountDown = -1.0;
            std::cout << "Stopped recording -- starting playback..." << std::endl;
            loadRecordedSkeletonData(g_fileNameForRecording);
            startPlayingSkeletonData(0);
        }
        else if (!isPlayingSkeletonData())
            g_recordingCountDown = Fubi::getCurrentTime() + 5.0f;
        break;
    case 'o':
        // Restart playback
        loadRecordedSkeletonData(g_fileNameForRecording);
        startPlayingSkeletonData();
        std::cout << "Starting playback of skeleton data..." << std::endl;
        break;
    }
    /*printf("Key-Code: %d\n", key);*/
}

int main(int argc, char ** argv)
{
    // OpenGL init
    glutInit(&argc, argv);
    glutInitDisplayMode(GLUT_RGB | GLUT_DOUBLE | GLUT_DEPTH);
    glutInitWindowSize(50, 50);
    g_window = glutCreateWindow("FUBI - Recognizer OpenGL test");
    glutKeyboardFunc(glutKeyboard);
    glutDisplayFunc(glutDisplay);
    glutIdleFunc(glutIdle);
    glDisable(GL_DEPTH_TEST);
    glEnable(GL_TEXTURE_2D);

    // Fubi init
    // Directly trying to start the next available sensor
    startNextSensor();
    // All known combination recognizers will be started automatically for new users
    setAutoStartCombinationRecognition(true);
    // Loading the sample recognizer definitions
    //loadRecognizersFromXML("SampleTemplateRecognizers.xml");
    loadRecognizersFromXML("SampleRecognizers.xml");
    loadRecognizersFromXML("SampleFingerRecognizers.xml");

    // Set callbacks for start and end of a recognition
    setRecognitionCallbacks(&recognitionStartCallback, &recognitionEndCallback);

    // Start the glut main loop, that constantly calls glutDisplay and glutIdle,
    // checks for keyboard events and exits the whole program if ESC is pressed
    glutMainLoop();

    // Usually it should never get here, unless something went wrong...
    // But nevertheless, release the OpenGL context
    glutDestroyWindow(g_window);
    // Release Fubi
    release();
    // And all allocated buffers
    delete[] g_depthData;
    delete[] g_rgbData;
    delete[] g_irData;
    return -1;
}
