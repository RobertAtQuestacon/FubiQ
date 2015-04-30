﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Video
{
    public class VideoClass
    {
        public string fileName = "test.vid";
        public bool playMode = false;
        public bool saveMode = false;
        public bool pause = false;
        private BinaryWriter aviWriter = null;
        private BinaryReader aviReader = null;
        private int playbackFrame = 0;

        public void readFrame(byte[] buffer, int frame_nr)
        {
            if (aviReader == null)
                return;
            if (aviReader.BaseStream.Position >= aviReader.BaseStream.Length)
            {
                playbackFrame = 0;
                aviReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            if (frame_nr >= 0 && frame_nr != playbackFrame)
            {
                playbackFrame = frame_nr;
                aviReader.BaseStream.Seek(buffer.Length * playbackFrame, SeekOrigin.Begin);
            }
            aviReader.Read(buffer, 0, buffer.Length);
            playbackFrame++;
        }

        public void saveFrame(byte[] buffer)
        {
            if (aviWriter != null)
                aviWriter.Write(buffer);
        }

        public void startPlayback()
        {

            if (aviReader != null)
            {
                ((IDisposable)aviReader).Dispose();
            }
            try
            {
                Console.WriteLine("Opening:" + fileName);
                aviReader = new BinaryReader(File.Open(fileName, FileMode.Open));
                playMode = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception " + ex.ToString() + " while opening " + fileName);
                aviReader = null;
            }
            pause = false;
        }

        public void startSave()
        {
            aviWriter = new BinaryWriter(File.Open(fileName, FileMode.Create));
            saveMode = true;
        }

        public void stopSave()
        {
            saveMode = false;
            if (aviWriter != null)
            {
                aviWriter.Close();
                ((IDisposable)aviWriter).Dispose();
                aviWriter = null;
            }
        }
        public void stopPlay()
        {
            playMode = false;
            if (aviReader != null)
            {
                aviReader.Close();
                ((IDisposable)aviReader).Dispose();
                aviReader = null;
            }

        }
    }
}