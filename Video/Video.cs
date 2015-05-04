using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

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
        private BufferedStream gzframeWriter = null;
        private MemoryStream compressIntoMs;
        private List<Int32> framePosList;
        private BinaryReader frameReader = null;
        private int playbackFrame = 0;
        const int BUFFER_SIZE = 640 * 480 * 4;

        public int readFrame(byte[] buffer, int frame_nr)
        {
            if (frameReader == null)
                return -1;
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
                frameReader.Read(comp_buffer, 0, frame_length);
                buffer = Decompress(comp_buffer);
                //using (var compressedMs = new MemoryStream(comp_buffer))
                //{
                //    using (var decompressedMs = new MemoryStream())
                //    {
                //        using (var gzs = new BufferedStream(new GZipStream(compressedMs,
                //         CompressionMode.Decompress), BUFFER_SIZE))
                //        {
                //            gzs.CopyTo(decompressedMs);
                //        }
                //        if (decompressedMs.Length != buffer.Length)
                //            return -1;  // error
                //        buffer = decompressedMs.ToArray();
                //    }
                //}
            }
            playbackFrame++;
            return playbackFrame;
        }

        public void saveFrame(byte[] buffer)
        {
            if (maxFrames > 0 && frameCount > maxFrames)
                return;
            //if (gzframeWriter != null)
            //{
            //    pos = gzframeWriter.Position;
            //    if (maxSizeKb > 0)
            //    {
            //        if ((pos >> 10) > maxSizeKb)
            //            return;
            //    }
            //    gzframeWriter.Write(buffer, 0, buffer.Length);
            //}
            //else
            if (frameWriter != null)
            {
                //pos = (Int32)frameWriter.BaseStream.Position; - didn't match the actual position due to streaming
                // acutally think it was okay now but the count the buffer length is cleaner anyhow.
                if (maxSizeMb > 0)
                {
                    if ((writePos >> 20) > maxSizeMb)
                        return;
                }
                if (toCompress)
                {
                    //using (var compressIntoMs = new MemoryStream())
                    //{
                    //    using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                    //     CompressionMode.Compress), BUFFER_SIZE))
                    //    {
                            gzframeWriter.Write(buffer, 0, buffer.Length);
                    //    }
                        buffer = compressIntoMs.ToArray();
                //    }
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
                if (fileName.EndsWith(".vic"))
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
            toCompress = fileName.EndsWith(".vic");
            frameCount = 0;
            writePos = 0;
            maxFrames = max_frames;
            maxSizeMb = max_size_mb;
            frameWriter = new BinaryWriter(File.Open(fileName, FileMode.Create));
              if (toCompress)
              {
                  framePosList = new List<Int32>();
                  compressIntoMs = new MemoryStream();
                  gzframeWriter = new BufferedStream(new GZipStream(compressIntoMs,
                     CompressionMode.Compress), BUFFER_SIZE);
              }
            saveMode = true;
        }

        public void stopSave()
        {
            saveMode = false;
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
            if (gzframeWriter != null)
            {
                gzframeWriter.Close();
                ((IDisposable)gzframeWriter).Dispose();
                gzframeWriter = null;
                compressIntoMs.Close();
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

        // test compress and decompress
        public static byte[] Compress(byte[] inputData)
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

        public static byte[] Decompress(byte[] inputData)
        {
            if (inputData == null)
                throw new ArgumentNullException("inputData must be non-null");

            using (var compressedMs = new MemoryStream(inputData))
            {
                using (var decompressedMs = new MemoryStream())
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

        public void compressTest()
        {
            byte[] test_string = new byte[4096];
            int i = 0;
            while (i < test_string.Length)
            {
                test_string[i] = (byte)(i & 255);
                i++;
            }
            Console.WriteLine("Length = " + test_string.Length);
            byte[] comp_string = Compress(test_string);
            Console.WriteLine("Length = " + comp_string.Length);
            byte[] out_string = Decompress(comp_string);
            Console.WriteLine("Length = " + out_string.Length);
        }

    }
}
