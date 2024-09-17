using System;
using System.IO;
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

            var audioClip = AudioClip.Create(fileName, soundData.Sound.Length / soundData.Channels, soundData.Channels, soundData.SampleRate, false);
            audioClip.SetData(soundData.Sound, 0);

            MultiTypeCache.AudioClips.Add(fileName, audioClip);
            return audioClip;
        }

        public static SoundData ConvertWavByteArrayToFloatArray(byte[] fileBytes)
        {
            //var riff = Encoding.ASCII.GetString(fileBytes, 0, 4);
            //var wave = Encoding.ASCII.GetString(fileBytes, 8, 4);
            var subchunk1 = BitConverter.ToInt32(fileBytes, 16);
            var audioFormat = BitConverter.ToUInt16(fileBytes, 20);

            var formatCode = FormatCode(audioFormat);

            var channels = BitConverter.ToUInt16(fileBytes, 22);
            var sampleRate = BitConverter.ToInt32(fileBytes, 24);
            //var byteRate = BitConverter.ToInt32(fileBytes, 28);
            //var blockAlign = BitConverter.ToUInt16(fileBytes, 32);
            var bitDepth = BitConverter.ToUInt16(fileBytes, 34);

            // Calculate header offset and data size
            var headerOffset = 20 + subchunk1;
            var dataSizeOffset = headerOffset + 4;
            if (dataSizeOffset + 4 > fileBytes.Length)
            {
                throw new ArgumentException("Invalid WAV file structure.");
            }

            var subchunk2 = BitConverter.ToInt32(fileBytes, dataSizeOffset);

            // Ensure that subchunk2 does not exceed fileBytes length
            var dataAvailable = fileBytes.Length - (dataSizeOffset + 4);
            if (subchunk2 > dataAvailable)
            {
                subchunk2 = dataAvailable;
            }

            if (formatCode == "IMA ADPCM")
            {
                return ConvertWavByteArrayToFloatArray(ImaadpcmDecoder.Decode(fileBytes));
            }

            // Copy WAV data section into a new array
            var data = new byte[subchunk2];
            Array.Copy(fileBytes, dataSizeOffset + 4, data, 0, subchunk2);

            return new SoundData
            {
                Sound = ConvertByteArrayToFloatArray(data, 0, (BitDepth)bitDepth),
                Channels = channels,
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
