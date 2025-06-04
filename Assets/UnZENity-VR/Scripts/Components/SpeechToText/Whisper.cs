using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using MyBox;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.VR.Components.SpeechToText
{
    public class Whisper
    {
        public bool IsInitialized;
        public bool IsTranscribing;
        public string OutputString;

        
        private Worker _decoder1, _decoder2, _encoder, _spectrogram;
        private Worker _argmax;

        private AudioClip _audioClip;

        // This is how many tokens you want. It can be adjusted.
        private const int _maxTokens = 100;

        // Special tokens see added tokens file for details
        private const int _endOfText = 50257;
        private const int _startOfTranscript = 50258;


        // https://huggingface.co/openai/whisper-tiny/blob/main/added_tokens.json

        private const int _cs = 50283; // <|cs|>
        private const int _de = 50261; // <|de|>
        private const int _en = 50259; // <|en|>
        private const int _es = 50262; // <|es|>
        private const int _fr = 50265; // <|fr|>
        private const int _it = 50274; // <|it|>
        private const int _pl = 50269; // <|pl|>
        private const int _ru = 50263; // <|ru|>
        
        private const int _transcribe = 50359; //for speech-to-text in specified language
        private const int _noTimeStamps = 50363;

        private int numSamples;
        private string[] tokens;

        private int tokenCount = 0;
        private NativeArray<int> _outputTokens;

        // Used for special character decoding
        private int[] _whiteSpaceCharacters = new int[256];

        private Tensor<float> _encodedAudio;
        
        // Maximum size of audioClip (30s at 16kHz)
        private const int _maxSamples = 30 * 16000;


        private bool _isFirstRun = true;
        public void Initialize()
        {
            // Already setup
            if (!_isFirstRun)
                return;

            _isFirstRun = false;
            try
            {
                var decoder1FilePath = $"{GetRootPath()}/decoder_model.onnx";
                var decoder2FilePath = $"{GetRootPath()}/decoder_with_past_model.onnx";
                var encoderFilePath = $"{GetRootPath()}/encoder_model.onnx";
                var logmelFilePath = $"{GetRootPath()}/logmel_spectrogram.onnx";
                var vocabFilePath = $"{GetRootPath()}/vocab.json";

                if (!File.Exists(decoder1FilePath) || !File.Exists(decoder2FilePath) || !File.Exists(encoderFilePath) ||
                    !File.Exists(logmelFilePath) || !File.Exists(vocabFilePath))
                {
                    Logger.Log("Whisper can't be initialized, as no LLM is found.", LogCat.VR);
                    return;
                }

                var audioDecoder1 = ModelLoader.Load(decoder1FilePath);
                var audioDecoder2 = ModelLoader.Load(decoder2FilePath);
                var audioEncoder = ModelLoader.Load(encoderFilePath);
                var logMelSpectro = ModelLoader.Load(logmelFilePath);
                vocabText = File.ReadAllText(vocabFilePath);

                if (audioDecoder1 == null || audioDecoder2 == null || audioEncoder == null || logMelSpectro == null ||
                    vocabText.IsNullOrEmpty())
                {
                    Logger.Log("Whisper can't be initialized, as LLM couldn't be initialized..", LogCat.VR);
                    return;
                }

                _decoder1 = new Worker(audioDecoder1, BackendType.GPUCompute);
                _decoder2 = new Worker(audioDecoder2, BackendType.GPUCompute);

                var graph = new FunctionalGraph();
                var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
                var amax = Functional.ArgMax(input, -1, false);
                var selectTokenModel = graph.Compile(amax);
                _argmax = new Worker(selectTokenModel, BackendType.GPUCompute);

                _encoder = new Worker(audioEncoder, BackendType.GPUCompute);
                _spectrogram = new Worker(logMelSpectro, BackendType.GPUCompute);

                _outputTokens = new NativeArray<int>(_maxTokens, Allocator.Persistent);

                _outputTokens[0] = _startOfTranscript;
                _outputTokens[1] = GetLanguageToken();
                _outputTokens[2] = _transcribe; //TRANSLATE;//
                //outputTokens[3] = NO_TIME_STAMPS;// START_TIME;//
                tokenCount = 3;

                SetupWhiteSpaceShifts();
                GetTokens();
            }
            catch (Exception e)
            {
                Logger.LogError($"Whisper can't be initialized: {e}", LogCat.VR);
                return;
            }
            
            IsInitialized = true;
        }
        
        public async void StartExec(AudioClip audioClip)
        {
            _audioClip = audioClip;

            IsTranscribing = false;
            OutputString = string.Empty;
            
            LoadAudio();
            EncodeAudio();
            IsTranscribing = true;

            tokensTensor = new Tensor<int>(new TensorShape(1, _maxTokens));
            ComputeTensorData.Pin(tokensTensor);
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload<int>(_outputTokens, tokenCount);

            lastToken = new NativeArray<int>(1, Allocator.Persistent); lastToken[0] = _noTimeStamps;
            lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { _noTimeStamps });

            while (true)
            {
                if (!IsTranscribing || tokenCount >= (_outputTokens.Length - 1))
                    return;
                m_Awaitable = InferenceStep();
                await m_Awaitable;
            }
        }

        private int GetLanguageToken()
        {
            switch (GameData.Language)
            {
                case "cs":
                    return _cs;
                case "de":
                    return _de;
                case "en":
                    return _en;
                case "es":
                    return _es;
                case "fr":
                    return _fr;
                case "it":
                    return _it;
                case "pl":
                    return _pl;
                case "ru":
                    return _ru;
                default:
                    Logger.LogWarning("No usable language for Whisper found. Fallback to >en<", LogCat.VR);
                    return _en;
            }
        }


        /// <summary>
        /// Return path of settings file based on target architecture.
        /// </summary>
        private string GetRootPath()
        {
            // https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
            // Will be: /storage/emulated/<userid>/Android/data/<packagename>/files
            if (Application.platform == RuntimePlatform.Android)
            {
                return Application.persistentDataPath + "/SpeechToText";
            }

            // https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html
            // Will be:
            // 1. Editor: Assets\StreamingAssets\
            // 2. Standalone: Build\Gothic-UnZENity_Data\StreamingAssets\
            return Application.streamingAssetsPath + "/SpeechToText";
        }
        
        
        
        
        
        
        
        Awaitable m_Awaitable;

        NativeArray<int> lastToken;
        Tensor<int> lastTokenTensor;
        Tensor<int> tokensTensor;
        Tensor<float> audioInput;

        void LoadAudio()
        {
            numSamples = _audioClip.samples;
            var data = new float[_maxSamples];
            numSamples = _maxSamples;
            _audioClip.GetData(data, 0);
            audioInput = new Tensor<float>(new TensorShape(1, numSamples), data);
        }

        void EncodeAudio()
        {
            _spectrogram.Schedule(audioInput);
            var logmel = _spectrogram.PeekOutput() as Tensor<float>;
            _encoder.Schedule(logmel);
            _encodedAudio = _encoder.PeekOutput() as Tensor<float>;
        }
        async Awaitable InferenceStep()
        {
            _decoder1.SetInput("input_ids", tokensTensor);
            _decoder1.SetInput("encoder_hidden_states", _encodedAudio);
            _decoder1.Schedule();

            var past_key_values_0_decoder_key = _decoder1.PeekOutput("present.0.decoder.key") as Tensor<float>;
            var past_key_values_0_decoder_value = _decoder1.PeekOutput("present.0.decoder.value") as Tensor<float>;
            var past_key_values_1_decoder_key = _decoder1.PeekOutput("present.1.decoder.key") as Tensor<float>;
            var past_key_values_1_decoder_value = _decoder1.PeekOutput("present.1.decoder.value") as Tensor<float>;
            var past_key_values_2_decoder_key = _decoder1.PeekOutput("present.2.decoder.key") as Tensor<float>;
            var past_key_values_2_decoder_value = _decoder1.PeekOutput("present.2.decoder.value") as Tensor<float>;
            var past_key_values_3_decoder_key = _decoder1.PeekOutput("present.3.decoder.key") as Tensor<float>;
            var past_key_values_3_decoder_value = _decoder1.PeekOutput("present.3.decoder.value") as Tensor<float>;

            var past_key_values_0_encoder_key = _decoder1.PeekOutput("present.0.encoder.key") as Tensor<float>;
            var past_key_values_0_encoder_value = _decoder1.PeekOutput("present.0.encoder.value") as Tensor<float>;
            var past_key_values_1_encoder_key = _decoder1.PeekOutput("present.1.encoder.key") as Tensor<float>;
            var past_key_values_1_encoder_value = _decoder1.PeekOutput("present.1.encoder.value") as Tensor<float>;
            var past_key_values_2_encoder_key = _decoder1.PeekOutput("present.2.encoder.key") as Tensor<float>;
            var past_key_values_2_encoder_value = _decoder1.PeekOutput("present.2.encoder.value") as Tensor<float>;
            var past_key_values_3_encoder_key = _decoder1.PeekOutput("present.3.encoder.key") as Tensor<float>;
            var past_key_values_3_encoder_value = _decoder1.PeekOutput("present.3.encoder.value") as Tensor<float>;

            _decoder2.SetInput("input_ids", lastTokenTensor);
            _decoder2.SetInput("past_key_values.0.decoder.key", past_key_values_0_decoder_key);
            _decoder2.SetInput("past_key_values.0.decoder.value", past_key_values_0_decoder_value);
            _decoder2.SetInput("past_key_values.1.decoder.key", past_key_values_1_decoder_key);
            _decoder2.SetInput("past_key_values.1.decoder.value", past_key_values_1_decoder_value);
            _decoder2.SetInput("past_key_values.2.decoder.key", past_key_values_2_decoder_key);
            _decoder2.SetInput("past_key_values.2.decoder.value", past_key_values_2_decoder_value);
            _decoder2.SetInput("past_key_values.3.decoder.key", past_key_values_3_decoder_key);
            _decoder2.SetInput("past_key_values.3.decoder.value", past_key_values_3_decoder_value);

            _decoder2.SetInput("past_key_values.0.encoder.key", past_key_values_0_encoder_key);
            _decoder2.SetInput("past_key_values.0.encoder.value", past_key_values_0_encoder_value);
            _decoder2.SetInput("past_key_values.1.encoder.key", past_key_values_1_encoder_key);
            _decoder2.SetInput("past_key_values.1.encoder.value", past_key_values_1_encoder_value);
            _decoder2.SetInput("past_key_values.2.encoder.key", past_key_values_2_encoder_key);
            _decoder2.SetInput("past_key_values.2.encoder.value", past_key_values_2_encoder_value);
            _decoder2.SetInput("past_key_values.3.encoder.key", past_key_values_3_encoder_key);
            _decoder2.SetInput("past_key_values.3.encoder.value", past_key_values_3_encoder_value);

            _decoder2.Schedule();

            var logits = _decoder2.PeekOutput("logits") as Tensor<float>;
            _argmax.Schedule(logits);
            using var t_Token = await _argmax.PeekOutput().ReadbackAndCloneAsync() as Tensor<int>;
            int index = t_Token[0];

            _outputTokens[tokenCount] = lastToken[0];
            lastToken[0] = index;
            tokenCount++;
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload(_outputTokens, tokenCount);
            lastTokenTensor.dataOnBackend.Upload(lastToken, 1);

            if (index == _endOfText)
            {
                IsTranscribing = false;
            }
            else if (index < tokens.Length)
            {
                OutputString += GetUnicodeText(tokens[index]);
            }
        }

        // Tokenizer
        public string vocabText;
        void GetTokens()
        {
            var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabText);
            tokens = new string[vocab.Count];
            foreach (var item in vocab)
            {
                tokens[item.Value] = item.Key;
            }
        }

        string GetUnicodeText(string text)
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
            return Encoding.UTF8.GetString(bytes);
        }

        string ShiftCharacterDown(string text)
        {
            string outText = "";
            foreach (char letter in text)
            {
                outText += ((int)letter <= 256) ? letter : (char)_whiteSpaceCharacters[(int)(letter - 256)];
            }
            return outText;
        }

        void SetupWhiteSpaceShifts()
        {
            for (int i = 0, n = 0; i < 256; i++)
            {
                if (IsWhiteSpace((char)i)) _whiteSpaceCharacters[n++] = i;
            }
        }

        bool IsWhiteSpace(char c)
        {
            return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
        }

        private void OnDestroy()
        {
            _decoder1.Dispose();
            _decoder2.Dispose();
            _encoder.Dispose();
            _spectrogram.Dispose();
            _argmax.Dispose();
            audioInput.Dispose();
            lastTokenTensor.Dispose();
            tokensTensor.Dispose();
        }
    }
}
