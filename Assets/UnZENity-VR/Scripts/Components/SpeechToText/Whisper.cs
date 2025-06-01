using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.InferenceEngine;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace GUZ.VR.Components
{
    public class Whisper : MonoBehaviour
    {
        Worker decoder1, decoder2, encoder, spectrogram;
        Worker argmax;

        private AudioClip _audioClip;

        // This is how many tokens you want. It can be adjusted.
        const int maxTokens = 100;

        // Special tokens see added tokens file for details
        const int END_OF_TEXT = 50257;
        const int START_OF_TRANSCRIPT = 50258;
        const int ENGLISH = 50259;
        const int GERMAN = 50261;
        const int FRENCH = 50265;
        const int TRANSCRIBE = 50359; //for speech-to-text in specified language
        const int TRANSLATE = 50358;  //for speech-to-text then translate to English
        const int NO_TIME_STAMPS = 50363;
        const int START_TIME = 50364;

        int numSamples;
        string[] tokens;

        int tokenCount = 0;
        NativeArray<int> outputTokens;

        // Used for special character decoding
        int[] whiteSpaceCharacters = new int[256];

        Tensor<float> encodedAudio;

        public bool IsTranscribing;
        public string OutputString;

        // Maximum size of audioClip (30s at 16kHz)
        const int maxSamples = 30 * 16000;

        public ModelAsset audioDecoder1, audioDecoder2;
        public ModelAsset audioEncoder;
        public ModelAsset logMelSpectro;

        public async void StartExec(AudioClip audioClip)
        {
            _audioClip = audioClip;

            IsTranscribing = false;
            OutputString = string.Empty;
            
            SetupWhiteSpaceShifts();
            GetTokens();

            decoder1 = new Worker(ModelLoader.Load(audioDecoder1), BackendType.GPUCompute);
            decoder2 = new Worker(ModelLoader.Load(audioDecoder2), BackendType.GPUCompute);

            FunctionalGraph graph = new FunctionalGraph();
            var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
            var amax = Functional.ArgMax(input, -1, false);
            var selectTokenModel = graph.Compile(amax);
            argmax = new Worker(selectTokenModel, BackendType.GPUCompute);

            encoder = new Worker(ModelLoader.Load(audioEncoder), BackendType.GPUCompute);
            spectrogram = new Worker(ModelLoader.Load(logMelSpectro), BackendType.GPUCompute);

            outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);

            outputTokens[0] = START_OF_TRANSCRIPT;
            outputTokens[1] = ENGLISH;// GERMAN;//FRENCH;//
            outputTokens[2] = TRANSCRIBE; //TRANSLATE;//
            //outputTokens[3] = NO_TIME_STAMPS;// START_TIME;//
            tokenCount = 3;

            LoadAudio();
            EncodeAudio();
            IsTranscribing = true;

            tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
            ComputeTensorData.Pin(tokensTensor);
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);

            lastToken = new NativeArray<int>(1, Allocator.Persistent); lastToken[0] = NO_TIME_STAMPS;
            lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

            while (true)
            {
                if (!IsTranscribing || tokenCount >= (outputTokens.Length - 1))
                    return;
                m_Awaitable = InferenceStep();
                await m_Awaitable;
            }
        }
        Awaitable m_Awaitable;

        NativeArray<int> lastToken;
        Tensor<int> lastTokenTensor;
        Tensor<int> tokensTensor;
        Tensor<float> audioInput;

        void LoadAudio()
        {
            numSamples = _audioClip.samples;
            var data = new float[maxSamples];
            numSamples = maxSamples;
            _audioClip.GetData(data, 0);
            audioInput = new Tensor<float>(new TensorShape(1, numSamples), data);
        }

        void EncodeAudio()
        {
            spectrogram.Schedule(audioInput);
            var logmel = spectrogram.PeekOutput() as Tensor<float>;
            encoder.Schedule(logmel);
            encodedAudio = encoder.PeekOutput() as Tensor<float>;
        }
        async Awaitable InferenceStep()
        {
            decoder1.SetInput("input_ids", tokensTensor);
            decoder1.SetInput("encoder_hidden_states", encodedAudio);
            decoder1.Schedule();

            var past_key_values_0_decoder_key = decoder1.PeekOutput("present.0.decoder.key") as Tensor<float>;
            var past_key_values_0_decoder_value = decoder1.PeekOutput("present.0.decoder.value") as Tensor<float>;
            var past_key_values_1_decoder_key = decoder1.PeekOutput("present.1.decoder.key") as Tensor<float>;
            var past_key_values_1_decoder_value = decoder1.PeekOutput("present.1.decoder.value") as Tensor<float>;
            var past_key_values_2_decoder_key = decoder1.PeekOutput("present.2.decoder.key") as Tensor<float>;
            var past_key_values_2_decoder_value = decoder1.PeekOutput("present.2.decoder.value") as Tensor<float>;
            var past_key_values_3_decoder_key = decoder1.PeekOutput("present.3.decoder.key") as Tensor<float>;
            var past_key_values_3_decoder_value = decoder1.PeekOutput("present.3.decoder.value") as Tensor<float>;

            var past_key_values_0_encoder_key = decoder1.PeekOutput("present.0.encoder.key") as Tensor<float>;
            var past_key_values_0_encoder_value = decoder1.PeekOutput("present.0.encoder.value") as Tensor<float>;
            var past_key_values_1_encoder_key = decoder1.PeekOutput("present.1.encoder.key") as Tensor<float>;
            var past_key_values_1_encoder_value = decoder1.PeekOutput("present.1.encoder.value") as Tensor<float>;
            var past_key_values_2_encoder_key = decoder1.PeekOutput("present.2.encoder.key") as Tensor<float>;
            var past_key_values_2_encoder_value = decoder1.PeekOutput("present.2.encoder.value") as Tensor<float>;
            var past_key_values_3_encoder_key = decoder1.PeekOutput("present.3.encoder.key") as Tensor<float>;
            var past_key_values_3_encoder_value = decoder1.PeekOutput("present.3.encoder.value") as Tensor<float>;

            decoder2.SetInput("input_ids", lastTokenTensor);
            decoder2.SetInput("past_key_values.0.decoder.key", past_key_values_0_decoder_key);
            decoder2.SetInput("past_key_values.0.decoder.value", past_key_values_0_decoder_value);
            decoder2.SetInput("past_key_values.1.decoder.key", past_key_values_1_decoder_key);
            decoder2.SetInput("past_key_values.1.decoder.value", past_key_values_1_decoder_value);
            decoder2.SetInput("past_key_values.2.decoder.key", past_key_values_2_decoder_key);
            decoder2.SetInput("past_key_values.2.decoder.value", past_key_values_2_decoder_value);
            decoder2.SetInput("past_key_values.3.decoder.key", past_key_values_3_decoder_key);
            decoder2.SetInput("past_key_values.3.decoder.value", past_key_values_3_decoder_value);

            decoder2.SetInput("past_key_values.0.encoder.key", past_key_values_0_encoder_key);
            decoder2.SetInput("past_key_values.0.encoder.value", past_key_values_0_encoder_value);
            decoder2.SetInput("past_key_values.1.encoder.key", past_key_values_1_encoder_key);
            decoder2.SetInput("past_key_values.1.encoder.value", past_key_values_1_encoder_value);
            decoder2.SetInput("past_key_values.2.encoder.key", past_key_values_2_encoder_key);
            decoder2.SetInput("past_key_values.2.encoder.value", past_key_values_2_encoder_value);
            decoder2.SetInput("past_key_values.3.encoder.key", past_key_values_3_encoder_key);
            decoder2.SetInput("past_key_values.3.encoder.value", past_key_values_3_encoder_value);

            decoder2.Schedule();

            var logits = decoder2.PeekOutput("logits") as Tensor<float>;
            argmax.Schedule(logits);
            using var t_Token = await argmax.PeekOutput().ReadbackAndCloneAsync() as Tensor<int>;
            int index = t_Token[0];

            outputTokens[tokenCount] = lastToken[0];
            lastToken[0] = index;
            tokenCount++;
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload(outputTokens, tokenCount);
            lastTokenTensor.dataOnBackend.Upload(lastToken, 1);

            if (index == END_OF_TEXT)
            {
                IsTranscribing = false;
            }
            else if (index < tokens.Length)
            {
                OutputString += GetUnicodeText(tokens[index]);
            }
        }

        // Tokenizer
        public TextAsset vocabAsset;
        void GetTokens()
        {
            var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabAsset.text);
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
                outText += ((int)letter <= 256) ? letter : (char)whiteSpaceCharacters[(int)(letter - 256)];
            }
            return outText;
        }

        void SetupWhiteSpaceShifts()
        {
            for (int i = 0, n = 0; i < 256; i++)
            {
                if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
            }
        }

        bool IsWhiteSpace(char c)
        {
            return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
        }

        private void OnDestroy()
        {
            decoder1.Dispose();
            decoder2.Dispose();
            encoder.Dispose();
            spectrogram.Dispose();
            argmax.Dispose();
            audioInput.Dispose();
            lastTokenTensor.Dispose();
            tokensTensor.Dispose();
        }
    }
}
