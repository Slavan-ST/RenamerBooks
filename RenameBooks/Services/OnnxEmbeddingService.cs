// Services/OnnxEmbeddingService.cs
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using System;
using System.IO;

namespace RenameBooks.Services
{
    public class OnnxEmbeddingService
    {
        private readonly MLContext _mlContext;
        private readonly ITransformer _model;
        private readonly PredictionEngine<InputData, OutputData> _predictionEngine;

        public OnnxEmbeddingService(string modelPath)
        {
            _mlContext = new MLContext();
            var pipeline = _mlContext.Transforms.ApplyOnnxModel(
                modelFile: modelPath,
                outputColumnNames: new[] { "sentence_embedding" },
                inputColumnNames: new[] { "input" }
            );
            _model = pipeline.Fit(_mlContext.Data.LoadFromEnumerable(new[] { new InputData() }));
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<InputData, OutputData>(_model);
        }

        public float[] GetEmbedding(string text)
        {
            var result = _predictionEngine.Predict(new InputData { Input = text ?? string.Empty });
            return result.SentenceEmbedding;
        }

        private class InputData
        {
            public string Input { get; set; } = string.Empty;
        }

        private class OutputData
        {
            [VectorType(384)] // MiniLM-L12-v2 → 384
            public float[] SentenceEmbedding { get; set; } = new float[384];
        }
    }
}