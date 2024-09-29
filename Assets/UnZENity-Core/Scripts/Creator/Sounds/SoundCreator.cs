using System;
using System.IO;
using System.Text;
using GUZ.Core.Caches;
using GUZ.Core.Data;
using JetBrains.Annotations;
using UnityEngine;

namespace GUZ.Core.Creator.Sounds
{
    public static class SoundCreator
    {
        private enum BitDepth
        {
            Bit8 = 8,
            Bit16 = 16
        }

        /// <summary>
        /// Create AudioClip from a file inside .vdf containers.
        /// Usage: ToAudioClip("fileName"):
        /// </summary>
        [CanBeNull]
        public static AudioClip ToAudioClip(string fileName)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);

            if (MultiTypeCache.AudioClips.TryGetValue(fileName, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            var soundData = ResourceLoader.TryGetSound(fileName);
            if (soundData == null)
            {
                return null;
            }

            var audioClip = AudioClip.Create(fileName, soundData.Sound.Length / soundData.Channels, soundData.Channels,
                soundData.SampleRate, false);
            audioClip.SetData(soundData.Sound, 0);

            MultiTypeCache.AudioClips.Add(fileName, audioClip);
            return audioClip;
        }

        public static SoundData ConvertWavByteArrayToFloatArray(byte[] fileBytes)
        {
            string riffHeader = Encoding.ASCII.GetString(fileBytes, 0, 4);

            int fileSize = BitConverter.ToInt32(fileBytes, 4);

            string waveHeader = Encoding.ASCII.GetString(fileBytes, 8, 4);

            string fmtHeader = Encoding.ASCII.GetString(fileBytes, 12, 4);

            int fmtLength = BitConverter.ToInt32(fileBytes, 16);

            ushort formatType = BitConverter.ToUInt16(fileBytes, 20);

            string formatCode = FormatCode(formatType);

            ushort numChannels = BitConverter.ToUInt16(fileBytes, 22);

            int sampleRate = BitConverter.ToInt32(fileBytes, 24);

            int byteRate = BitConverter.ToInt32(fileBytes, 28);

            short blockAlign = BitConverter.ToInt16(fileBytes, 32);

            short bitsPerSample = BitConverter.ToInt16(fileBytes, 34);
            
            string dataHeader = Encoding.ASCII.GetString(fileBytes, 36, 4);
            
            // Check for "PAD" header and skip it if present
            int padSize = 0;
            while (dataHeader == "PAD ")
            {
                padSize = padSize + BitConverter.ToInt32(fileBytes, 40);
                
                // we add 8 bits to padding as to skip the pad subchunk header + data
                padSize = padSize + 8;
                
                // Skip the PAD section
                dataHeader = Encoding.ASCII.GetString(fileBytes, 36 + padSize, 4);
            }

            int dataSize = BitConverter.ToInt32(fileBytes, 40 + padSize);

            if (formatCode == "IMA ADPCM")
            {
                return ConvertWavByteArrayToFloatArray(ImaadpcmDecoder.Decode(fileBytes));
            }
            
            // sometimes a file has more data than is specified after the RIFF header
            long stopPosition = Math.Min(dataSize, (fileBytes.Length - 44));

            // Copy WAV data section into a new array
            var audioData = new byte[stopPosition];
            Array.Copy(fileBytes, 44+padSize, audioData, 0, stopPosition);

            return new SoundData
            {
                Sound = ConvertByteArrayToFloatArray(audioData, 0, (BitDepth)bitsPerSample),
                Channels = numChannels,
                SampleRate = sampleRate
            };
        }


        private static float[] ConvertByteArrayToFloatArray(byte[] source, int headerOffset, BitDepth bit)
        {
            switch (bit)
            {
                case BitDepth.Bit8:
                    {
                        var wavSize = BitConverter.ToInt32(source, headerOffset);

                        var data = new float[wavSize];

                        var maxValue = sbyte.MaxValue;

                        for (var i = 0; i < wavSize; i++)
                        {
                            data[i] = (float)source[i] / maxValue;
                        }

                        return data;
                    }
                case BitDepth.Bit16:
                    {
                        var bytesPerSample = sizeof(short); // block size = 2
                        var sampleCount = source.Length / bytesPerSample;

                        var data = new float[sampleCount];

                        var maxValue = short.MaxValue;

                        for (var i = 0; i < sampleCount; i++)
                        {
                            var offset = i * bytesPerSample;
                            var sample = BitConverter.ToInt16(source, offset);
                            var floatSample = (float)sample / maxValue;
                            data[i] = floatSample;
                        }

                        return data;
                    }
                default:
                    throw new Exception(bit + " bit depth is not supported.");
            }
        }

        private static string FormatCode(ushort code)
        {
            switch (code)
            {
                case 1:
                    return "PCM";
                case 2:
                    return "ADPCM";
                case 3:
                    return "IEEE";
                case 7:
                    return "μ-law";
                case 17:
                    return "IMA ADPCM";
                case 65534:
                    return "WaveFormatExtensable";
                default:
                    return "";
            }
        }
    }
}
