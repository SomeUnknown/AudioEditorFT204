﻿using System;
using System.IO;
using Audio_Editor.Infrastructure;
using NAudio.Wave;

namespace Audio_Editor.Domain
{
    public class ReverseFile
    {
        const int _bitsPerByte = 8;
        static int _bytesPerSample;

        public static void Start(string forwardsWavFilePath)
        {
            byte[] forwardsWavFileStreamByteArray = populateForwardsWavFileByteArray(forwardsWavFilePath);
            getWavMetadata(forwardsWavFileStreamByteArray);
            int startIndexOfDataChunk = getStartIndexOfDataChunk(forwardsWavFileStreamByteArray);
            byte[] reversedWavFileStreamByteArray = populateReversedWavFileByteArray(forwardsWavFileStreamByteArray, startIndexOfDataChunk, _bytesPerSample);
            writeReversedWavFileByteArrayToFile(reversedWavFileStreamByteArray);
        }

        private static void getWavMetadata(byte[] forwardsWavFileStreamByteArray)
        {
            MetadataReverser.GetRiffText(forwardsWavFileStreamByteArray);
            MetadataReverser.GetFileSize(forwardsWavFileStreamByteArray);
            MetadataReverser.GetWaveText(forwardsWavFileStreamByteArray);
            MetadataReverser.GetFmtText(forwardsWavFileStreamByteArray);
            MetadataReverser.GetLengthOfFormatData(forwardsWavFileStreamByteArray);
            MetadataReverser.GetTypeOfFormat(forwardsWavFileStreamByteArray);
            MetadataReverser.GetNumOfChannels(forwardsWavFileStreamByteArray);
            MetadataReverser.GetSampleRate(forwardsWavFileStreamByteArray);
            MetadataReverser.GetBytesPerSecond(forwardsWavFileStreamByteArray);
            MetadataReverser.GetBlockAlign(forwardsWavFileStreamByteArray);
            _bytesPerSample = MetadataReverser.GetBitsPerSample(forwardsWavFileStreamByteArray) / _bitsPerByte;
            MetadataReverser.GetListText(forwardsWavFileStreamByteArray);
            MetadataReverser.GetDataText(forwardsWavFileStreamByteArray);
            MetadataReverser.GetDataSize(forwardsWavFileStreamByteArray);
        }

        private static void writeReversedWavFileByteArrayToFile(byte[] reversedWavFileStreamByteArray)
        {
            using (var reversedFileStream = new FileStream(Environment.CurrentDirectory + @"\temp" + Globals.index + ".wav", FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                reversedFileStream.Write(reversedWavFileStreamByteArray, 0, reversedWavFileStreamByteArray.Length);
            }
            Globals.reader = new MediaFoundationReader(Environment.CurrentDirectory + @"\temp" + Globals.index + ".wav");
            Globals.player.Open(new Uri(Environment.CurrentDirectory + @"\temp" + Globals.index + ".wav"));
            Globals.path = Environment.CurrentDirectory + @"\temp" + Globals.index + ".wav";
            Globals.index += 1;
        }

        private static byte[] populateReversedWavFileByteArray(byte[] forwardsWavFileStreamByteArray, int startIndexOfDataChunk, int bytesPerSample)
        {
            byte[] forwardsArrayWithOnlyHeaders = createForwardsArrayWithOnlyHeaders(forwardsWavFileStreamByteArray, startIndexOfDataChunk);

            byte[] forwardsArrayWithOnlyAudioData = createForwardsArrayWithOnlyAudioData(forwardsWavFileStreamByteArray, startIndexOfDataChunk);

            byte[] reversedArrayWithOnlyAudioData = reverseTheForwardsArrayWithOnlyAudioData(bytesPerSample, forwardsArrayWithOnlyAudioData);

            byte[] reversedWavFileStreamByteArray = combineArrays(forwardsArrayWithOnlyHeaders, reversedArrayWithOnlyAudioData);

            return reversedWavFileStreamByteArray;
        }

        private static byte[] combineArrays(byte[] forwardsArrayWithOnlyHeaders, byte[] reversedArrayWithOnlyAudioData)
        {
            byte[] reversedWavFileStreamByteArray = new byte[forwardsArrayWithOnlyHeaders.Length + reversedArrayWithOnlyAudioData.Length];
            Array.Copy(forwardsArrayWithOnlyHeaders, reversedWavFileStreamByteArray, forwardsArrayWithOnlyHeaders.Length);
            Array.Copy(reversedArrayWithOnlyAudioData, 0, reversedWavFileStreamByteArray, forwardsArrayWithOnlyHeaders.Length, reversedArrayWithOnlyAudioData.Length);
            return reversedWavFileStreamByteArray;
        }

        private static byte[] reverseTheForwardsArrayWithOnlyAudioData(int bytesPerSample, byte[] forwardsArrayWithOnlyAudioData)
        {
            int length = forwardsArrayWithOnlyAudioData.Length;
            byte[] reversedArrayWithOnlyAudioData = new byte[length];

            int sampleIdentifier = 0;

            for (int i = 0; i < length; i++)
            {
                if (i != 0 && i % bytesPerSample == 0)
                {
                    sampleIdentifier += 2 * bytesPerSample;
                }
                int index = length - bytesPerSample - sampleIdentifier + i;
                reversedArrayWithOnlyAudioData[i] = forwardsArrayWithOnlyAudioData[index];
            }
            return reversedArrayWithOnlyAudioData;
        }

        private static byte[] createForwardsArrayWithOnlyAudioData(byte[] forwardsWavFileStreamByteArray, int startIndexOfDataChunk)
        {
            byte[] forwardsArrayWithOnlyAudioData = new byte[forwardsWavFileStreamByteArray.Length - startIndexOfDataChunk];
            Array.Copy(forwardsWavFileStreamByteArray, startIndexOfDataChunk, forwardsArrayWithOnlyAudioData, 0, forwardsWavFileStreamByteArray.Length - startIndexOfDataChunk);
            return forwardsArrayWithOnlyAudioData;
        }

        private static byte[] createForwardsArrayWithOnlyHeaders(byte[] forwardsWavFileStreamByteArray, int startIndexOfDataChunk)
        {
            byte[] forwardsArrayWithOnlyHeaders = new byte[startIndexOfDataChunk];
            Array.Copy(forwardsWavFileStreamByteArray, 0, forwardsArrayWithOnlyHeaders, 0, startIndexOfDataChunk);
            return forwardsArrayWithOnlyHeaders;
        }

        private static byte[] populateForwardsWavFileByteArray(string forwardsWavFilePath)
        {
            FileStream forwardsWavFileStream = new FileStream(forwardsWavFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] forwardsWavFileStreamByteArray = new byte[forwardsWavFileStream.Length];
            forwardsWavFileStream.Read(forwardsWavFileStreamByteArray, 0, (int)forwardsWavFileStream.Length);
            return forwardsWavFileStreamByteArray;
        }

        private static int getStartIndexOfDataChunk(byte[] forwardsWavFileStreamByteArray)
        {
            int startIndexOfAudioData = 12;
            int charDAsciiDecimalCode = 100;
            int charAAsciiDecimalCode = 97;
            int charTAsciiDecimalCode = 116;
            int chunkSize;
            while (!(forwardsWavFileStreamByteArray[startIndexOfAudioData] == charDAsciiDecimalCode && forwardsWavFileStreamByteArray[startIndexOfAudioData + 1] == charAAsciiDecimalCode && forwardsWavFileStreamByteArray[startIndexOfAudioData + 2] == charTAsciiDecimalCode && forwardsWavFileStreamByteArray[startIndexOfAudioData + 3] == charAAsciiDecimalCode))
            {
                startIndexOfAudioData += 4;
                chunkSize = forwardsWavFileStreamByteArray[startIndexOfAudioData] + forwardsWavFileStreamByteArray[startIndexOfAudioData + 1] * 256 + forwardsWavFileStreamByteArray[startIndexOfAudioData + 2] * 65536 + forwardsWavFileStreamByteArray[startIndexOfAudioData + 3] * 16777216;
                startIndexOfAudioData += 4 + chunkSize;
            }
            startIndexOfAudioData += 8;
            return startIndexOfAudioData;
        }
    }
}