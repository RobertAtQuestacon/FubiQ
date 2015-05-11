using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

//using System.Windows.Media.Imaging;   // needs reference to Presentation Core and possible WindowsBase
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;  // for Marshall

using AForge.Video;
using AForge.Video.VFW;

namespace Video
{
    public class VideoClass
    {
        public string fileName = "test.vid";
        public bool playMode = false;
        public bool saveMode = false;
        private bool toCompress = false;
        public Int32 frameCount = 0;
        public Int32 writePos = 0;
        private int maxFrames = 0;
        private int maxSizeMb = 0;
        public bool pause = false;
        private BinaryWriter frameWriter = null;
        private List<Int32> framePosList;
        private BinaryReader frameReader = null;
        private int playbackFrame = 0;
        private int frameWidth = 640;
        private int frameHeight = 480;
        private int frameChannels = 4;
        const int BUFFER_SIZE = 640 * 480 * 4;
        public AVIWriter aviWriter = null;
        public bool edgeFilterEnabled = true;
        public UInt32 blockFillColor = 0xff8080ff;  // light blue
        public int edgeDelta = 2;
        public int edgeThickness = 2;

        public VideoClass() {
          //gzCompressTest();
            rleCompressTest();
        }

        // test compress and decompress
        public static byte[] gzCompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressIntoMs = new MemoryStream())
            {
                using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                 CompressionMode.Compress), BUFFER_SIZE))
                {
                    gzs.Write(inputData, 0, inputData.Length);
                }
                return compressIntoMs.ToArray();
            }
        }

        public static byte[] gzDecompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressedMs = new MemoryStream(inputData))
            {
                using (var decompressedMs = new MemoryStream(BUFFER_SIZE))
                {
                    using (var gzs = new BufferedStream(new GZipStream(compressedMs,
                     CompressionMode.Decompress), BUFFER_SIZE))
                    {
                        gzs.CopyTo(decompressedMs);
                    }
                    return decompressedMs.ToArray();
                }
            }
        }

        public void gzCompressTest()
        {
            byte[] test_string = new byte[1228800];
            int i = 0;
            while (i < test_string.Length)
            {
                test_string[i] = (byte)(i & 255);
                i++;
            }
            //byte[] comp_string = { 31, 138, 8, 0, 0, 0, 0, 0, 4, 0 };
            Console.WriteLine("Length = " + test_string.Length);
            byte[] comp_string = gzCompress(test_string);
            Console.WriteLine("Length = " + comp_string.Length);
            byte[] out_string = gzDecompress(comp_string);
            Console.WriteLine("Length = " + out_string.Length);
        }

        public byte[] rleCompress(byte[] bufin)
        {         
            using (MemoryStream memStream = new MemoryStream(bufin.Length))
            {
                // first save length of the uncompressed array
                memStream.Write(BitConverter.GetBytes((Int32)bufin.Length), 0, 4);
                UInt32 last_pix = 0;
                int pix_count = 0;
                int start_mixed = 0;
                int end_mixed = 0;
                bool save_mixed = false;
                for (int i = 0; i < bufin.Length; i += 4)
                {
                    UInt32 pix = BitConverter.ToUInt32(bufin, i);
                    //Console.WriteLine("Pos:" + i.ToString() + " = " + pix.ToString("x")); // + "  MixCount="+mix_count);
                    // test pix = 0xFF0000FF;
                    if ((pix != last_pix) || (pix_count >= Int16.MaxValue))
                    {   // not the same this time
                        // first, do we need to save a run of same pixels?
                        while (pix_count > 1)  // 2 or more pixels were the same
                        {
                            //Console.WriteLine("Found run of " + pix_count.ToString() + " pixels of value:" + last_pix.ToString("x"));
                            memStream.Write(BitConverter.GetBytes((Int16)Math.Min(pix_count,Int16.MaxValue)), 0, 2);
                            memStream.Write(BitConverter.GetBytes(last_pix), 0, 4);
                            if (pix_count >= Int16.MaxValue)
                                pix_count -= Int16.MaxValue;
                            else
                                pix_count = 1;
                            start_mixed = i;
                        }
                        if (pix != last_pix)
                            end_mixed = i + 4; // current pixel is include in mixed bag until proven otherwise
                        last_pix = pix;
                    }
                    else  // last pix is same as this one
                    {
                        if(i == end_mixed)
                          end_mixed = i - 4;  // last pixel is part of a run
                        if (end_mixed > start_mixed)  
                        {
                            save_mixed = true;  // if a run is starting then better save the mixed bag
                        }
                        pix_count++;
                    }
                    int mix_count = (end_mixed - start_mixed) / 4;  // range is exclusive so if start and end are same then 1 pixel
                    if (mix_count >= Int16.MaxValue)
                    {
                        save_mixed = true;
                    }
                    if ((mix_count > 0) && (i >= bufin.Length - 4))  // last pixel was part of a mixed bag
                    {
                        //mix_count++;
                        save_mixed = true;
                    }
                    if (save_mixed)
                    {
                        //Console.WriteLine("Found mixed group of " + mix_count.ToString() + " pixels");
                        memStream.Write(BitConverter.GetBytes((UInt16)Math.Min(mix_count, Int16.MaxValue) | 0x8000), 0, 2);
                        for (int j = start_mixed; j < end_mixed; j += 4)
                        {
                            UInt32 pix_old = BitConverter.ToUInt32(bufin, j);
                            memStream.Write(BitConverter.GetBytes(pix_old), 0, 4);
                        }
                        start_mixed = i;
                        save_mixed = false;
                    }

                }
                if (pix_count > 1)  // 2 or more pixels were the same at the end of the array
                {
                    //Console.WriteLine("Found run of " + pix_count.ToString() + " pixels of value:" + last_pix.ToString("x"));
                    memStream.Write(BitConverter.GetBytes((Int16)Math.Min(pix_count, Int16.MaxValue)), 0, 2);
                    memStream.Write(BitConverter.GetBytes(last_pix), 0, 4);
                }
                return memStream.ToArray();
            }
        }

        public byte[] rleDecompress(byte[] bufin)
        {
            int out_size = (int)BitConverter.ToInt32(bufin, 0);
            using (MemoryStream memStream = new MemoryStream(out_size))
            {
                for (int i = 4; i < bufin.Length; i += 2)
                {
                    UInt16 rlc = BitConverter.ToUInt16(bufin, i);
                    int rl = rlc & 0x7FFF;
                    if ((rlc & 0x8000) == 0)  // rl 32 bit pixels of the same
                    {
                        i += 2;
                        UInt32 pix_l = BitConverter.ToUInt16(bufin, i);
                        i += 2;
                        UInt32 pix_h = BitConverter.ToUInt16(bufin, i);
                        UInt32 pix = pix_l | (pix_h << 16);
                        for (int j = 0; j < rl; j++)
                        {
                            memStream.Write(BitConverter.GetBytes(pix), 0, 4);
                        }
                    }
                    else
                    {   // rl 32 bit pixels all different
                        for (int j = 0; j < rl; j++)
                        {
                            i += 2;
                            UInt32 pix_l = BitConverter.ToUInt16(bufin, i);
                            i += 2;
                            UInt32 pix_h = BitConverter.ToUInt16(bufin, i);
                            UInt32 pix = pix_l | (pix_h << 16);
                            memStream.Write(BitConverter.GetBytes(pix), 0, 4);
                        }
                    }
                }
                return memStream.ToArray();
            }
        }


        public void rleCompressTest()
        {
            //byte[] test_string = new byte[1228800];
            //int i = 0;
            //while (i < test_string.Length)
            //{
            //    test_string[i] = (byte)(i & 255);
            //    i++;
            //}
            byte[] test_string = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 1, 5, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0 };
            Console.WriteLine("Length = " + test_string.Length);
            byte[] comp_string = rleCompress(test_string);
            Console.WriteLine("Length = " + comp_string.Length);
            byte[] out_string = rleDecompress(comp_string);
            Console.WriteLine("Length = " + out_string.Length);
        }

        public byte[] edgeFilter(byte[] bufin)
        {         
            using (MemoryStream memStream = new MemoryStream(bufin.Length))
            {
                int last_depth = 1000;
                int edgeDelay = 0;
                for (int i = 0; i < bufin.Length; i += 4)
                {
                    UInt32 pix = BitConverter.ToUInt32(bufin, i);
                    int depth; 
                    if (bufin[i] == 0)
                    {
                        pix = 0;  
                        depth = 1000;
                    }
                    else  {
                        pix = blockFillColor;
                       depth = bufin[i + 1] + bufin[i + 2] + bufin[i + 3];  // r + g +  b
                    }
                    int dd = Math.Abs(depth - last_depth);
                    if (dd > edgeDelta)
                        edgeDelay = edgeThickness;
                    if (edgeDelay > 0)
                    {
                        pix = 0xff000000;
                        edgeDelay--;
                    }
                    last_depth = depth;
                    memStream.Write(BitConverter.GetBytes(pix), 0, 4);
                }
                return memStream.ToArray();
            }
        }

        public int readFrame(byte[] buffer, int frame_nr)
        {
            if (frameReader == null)
                return -1;
            try
            {
                Int32 frame_start = 0;
                if (frameReader.BaseStream.Position >= frameReader.BaseStream.Length)
                {
                    playbackFrame = 0;
                    frameReader.BaseStream.Seek(0, SeekOrigin.Begin);
                }
                else if ((frame_nr >= 0 && (frame_nr != playbackFrame)) || framePosList != null)
                {
                    if (frame_nr < 0)
                        frame_nr = 0;
                    playbackFrame = frame_nr;
                    if (framePosList == null || playbackFrame == 0)
                        frameReader.BaseStream.Seek(buffer.Length * playbackFrame, SeekOrigin.Begin);
                    else
                    {
                        frame_start = framePosList[playbackFrame - 1];
                        frameReader.BaseStream.Seek(frame_start, SeekOrigin.Begin);
                    }
                }
                if (framePosList == null)
                    frameReader.Read(buffer, 0, buffer.Length);
                else
                {
                    int frame_length = framePosList[playbackFrame] - frame_start;
                    byte[] comp_buffer = new byte[frame_length];
                    int br = frameReader.Read(comp_buffer, 0, frame_length);
                    Console.WriteLine("Bytes read:" + br + " @ " + frameReader.BaseStream.Position);
                    if (br > 0)
                    {
                        if (fileName.EndsWith(".vgz"))
                            Buffer.BlockCopy(gzDecompress(comp_buffer), 0, buffer, 0, buffer.Length); 
                        else
                            Buffer.BlockCopy(rleDecompress(comp_buffer), 0, buffer, 0, buffer.Length);
                    }
                    else
                    {
                        new Random().NextBytes(buffer);  //static
                    }
                }
                playbackFrame++;
                //Console.WriteLine("Frame:" + playbackFrame);
                return playbackFrame;
            }
            catch (Exception e)
            {
                Console.WriteLine("Frame exception: " + e.ToString());
            }
            return -1;
        }

        public void saveFrame(byte[] buffer)
        {


            if (maxFrames > 0 && frameCount > maxFrames)
            {
                Console.WriteLine("MaxFrames exceeded");
                return;
            }

            if (aviWriter != null)
            {
                Bitmap bitmap = new Bitmap(640, 480, PixelFormat.Format32bppArgb);
                BitmapData bmData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                IntPtr pNative = bmData.Scan0;
                Marshal.Copy(buffer, 0, pNative, frameWidth * frameHeight * frameChannels);
                bitmap.UnlockBits(bmData);
                aviWriter.AddFrame(bitmap);

            }
            else if (frameWriter != null)
            {
                //pos = (Int32)frameWriter.BaseStream.Position; - didn't match the actual position due to streaming
                // acutally think it was okay now but the count the buffer length is cleaner anyhow.
                if (maxSizeMb > 0)
                {
                    if ((writePos >> 20) > maxSizeMb)
                    {
                        Console.WriteLine("MaxSize exceeded");
                        return;
                    }
                }
                buffer = edgeFilter(buffer);
                if (toCompress)
                {
                    if (fileName.EndsWith(".vgz"))
                    {
                        //gzframeWriter.Write(buffer, 0, buffer.Length);
                        //    }
                        //buffer = compressIntoMs.ToArray();
                        buffer = gzCompress(buffer);
                    }
                    else
                    {
                        buffer = rleCompress(buffer);
                    }
                }
                frameWriter.Write(buffer);
                writePos += buffer.Length;
                framePosList.Add(writePos);
                frameCount++;
            }
        }

        public void startPlayback()
        {

            if (frameReader != null)
            {
                ((IDisposable)frameReader).Dispose();
            }
            try
            {
                Console.WriteLine("Opening:" + fileName);
                frameReader = new BinaryReader(File.Open(fileName, FileMode.Open));
                playMode = true;
                if (!fileName.EndsWith(".vid"))  // compression of some sort
                {
                    frameReader.BaseStream.Seek(-sizeof(Int32), SeekOrigin.End);
                    long frame_index_pos = frameReader.ReadInt32();
                    frameReader.BaseStream.Seek(frame_index_pos, SeekOrigin.Begin);
                    framePosList = new List<Int32>();
                    while (frameReader.BaseStream.Position < frameReader.BaseStream.Length)
                    {
                        Int32 fp = frameReader.ReadInt32();
                        framePosList.Add(fp);
                    }
                    frameReader.BaseStream.Seek(0, SeekOrigin.Begin);
                }
                else
                    framePosList = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex.ToString() + " while opening " + fileName);
                frameReader = null;
            }
            pause = false;
        }

        public void startSave(int max_frames = 1000, int max_size_mb = 1000)
        // buffer size is only required if compress is true
        {
            frameCount = 0;
            if (fileName.EndsWith(".avi"))
            {
                aviWriter = new AVIWriter("MRLE");
                aviWriter.FrameRate = 30;
                Console.WriteLine("Codec:" + aviWriter.Codec);
                if (File.Exists(fileName))
                    File.Delete(fileName);  // otherwise it will not run
                aviWriter.Open(fileName, Convert.ToInt32(640), Convert.ToInt32(480));
                // always crashes here anyway - something to do with Windows7 I think
                // found some advice at http://code.google.com/p/aforge/issues/detail?id=312

                /*
                 * Type 	Name 	          Format Binary 	        Version
                   ICM 	Microsoft RLE 	  MRLE 	msrle32.dll 	6.1.7600.16490
                   ICM 	Microsoft Video 1 MSVC 	msvidc32.dll 	6.1.7600.16490
                   ICM 	Microsoft YUV 	  UYVY 	msyuv.dll 	6.1.7600.16490
                   ICM 	Intel IYUV codec  IYUV 	iyuv_32.dll 	6.1.7600.16490
                   ICM 	Toshiba YUV Codec 	Y411 	tsbyuv.dll 	6.1.7600.16490
                   ICM 	Cinepak Codec by Radius 	cvid 	iccvid.dll 	1.10.0.13
                   DMO 	Mpeg4s Decoder DMO 	mp4s, MP4S, m4s2, M4S2, MP4V, mp4v, XVID, xvid, DIVX, DX50 	mp4sdecd.dll 	6.1.7600.16385
                   DMO 	WMV Screen decoder DMO 	MSS1, MSS2 	wmvsdecd.dll 	6.1.7600.16385
                   DMO 	WMVideo Decoder DMO 	WMV1, WMV2, WMV3, WMVA, WVC1, WMVP, WVP2 	wmvdecod.dll 	6.1.7600.16385
                   DMO 	Mpeg43 Decoder DMO 	mp43, MP43 	mp43decd.dll 	6.1.7600.16385
                   DMO 	Mpeg4 Decoder DMO 	MPG4, mpg4, mp42, MP42 	mpg4decd.dll 	6.1.7600.16385
                 * */
            }
            else
            {
                toCompress = !fileName.EndsWith(".vid");
                writePos = 0;
                maxFrames = max_frames;
                maxSizeMb = max_size_mb;
                frameWriter = new BinaryWriter(File.Open(fileName, FileMode.Create));
                if (toCompress)
                {
                    framePosList = new List<Int32>();
                }
                saveMode = true;
            }
        }

        public void stopSave()
        {
            saveMode = false;
            if (aviWriter != null)
            {
                aviWriter.Close();
            }
            if (frameWriter != null)
            {
                if (framePosList != null && toCompress)
                {  // need to save frame positions and postion of start of frame positions
                    //Int32 pos = (Int32)frameWriter.BaseStream.Position;
                    foreach (Int32 fp in framePosList)
                    {
                        frameWriter.Write(fp);
                    }
                    frameWriter.Write(writePos);  // which is also the end of the last frame
                }
                frameWriter.Close();
                ((IDisposable)frameWriter).Dispose();
                frameWriter = null;
            }
        }
        public void stopPlay()
        {
            playMode = false;
            if (frameReader != null)
            {
                frameReader.Close();
                ((IDisposable)frameReader).Dispose();
                frameReader = null;
            }
        }



    }
}
