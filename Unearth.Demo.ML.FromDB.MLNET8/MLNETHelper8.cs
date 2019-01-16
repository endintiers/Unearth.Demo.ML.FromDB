using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Core.Data;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Runtime.Data;
using System;
using System.IO;
using System.Linq;
using Unearth.Demo.ML.FromDB.Common.Sql;
using Unearth.Demo.ML.FromDB.MLNET8.Models;

namespace Unearth.Demo.ML.FromDB.MLNET8
{
    public class MLNETHelper8
    {
        public static ITransformer TrainModel(string connectionString = null, int nth = 1)
        {
            ITransformer trainedModel = null;

            // Create an ML.NET environment
            var mlContext = new MLContext(seed: 0);

            // Train from data in the DB
            var optionsBuilder = new DbContextOptionsBuilder<AirlinesContext>();
            optionsBuilder.UseSqlServer(connectionString);
            using (var airlinesModel = new AirlinesContext(optionsBuilder.Options))
            {
                Console.WriteLine("Create an enumerable view of the DB training data");
                var flightCodeTrainingData = airlinesModel.FlightCodes.Where(fc => fc.Id % nth == 0).AsEnumerable()
                    .Select(f => new FlightCodeFeatures()
                    {
                        FlightCode = f.FlightCode,
                        IATACode = f.Iatacode
                    });
                var trainingDataView = mlContext.CreateStreamingDataView(flightCodeTrainingData);

                var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("IATACode", "Label")
                    .Append(mlContext.Transforms.Text.FeaturizeText("FlightCode", "FlightCodeFeaturized"))
                    .Append(mlContext.Transforms.Concatenate("Features", "FlightCodeFeaturized"));

                Console.WriteLine("Training the ML.NET 0.8 model");

                var trainer = mlContext.MulticlassClassification.Trainers.StochasticDualCoordinateAscent(DefaultColumnNames.Label, DefaultColumnNames.Features);

                // Set the trainer and map prediction to a string (one of the original labels)
                var trainingPipeline = dataProcessPipeline.Append(trainer)
                        .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                // Do the actual training, reads the features and builds the model
                var watch = System.Diagnostics.Stopwatch.StartNew();
                trainedModel = trainingPipeline.Fit(trainingDataView);
                watch.Stop();
                long elapsedMs = watch.ElapsedMilliseconds;
                Console.WriteLine($"Training took {elapsedMs / 1000f} secs");
                Console.WriteLine();
            }

            return trainedModel;
        }

        public static float TestModel(ITransformer model)
        {
            // Create an ML.NET environment
            var mlContext = new MLContext(seed: 0);

            // Make a predictor using the trained model
            var flightCodePredictor = model.MakePredictionFunction<FlightCodeFeatures, FlightCodePrediction>(mlContext);

            // Test the predictor (on data not used for training)
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Predicting IATA Aircraft Codes");
            Console.ForegroundColor = defaultColor;

            var correct = 0;
            var incorrect = 0;

            using (TextReader reader = new StreamReader(@"TrainingData\MoreFlightCodes.csv"))
            {
                var csvReader = new CsvReader(reader);
                var records = csvReader.GetRecords<FlightCodeFeatures>();
                foreach (var rec in records)
                {
                    var prediction = flightCodePredictor.Predict(rec);
                    if (prediction.IATACode == rec.IATACode)
                    {
                        correct++;
                        if (correct % 300 == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"FlightCode: {rec.FlightCode}, Aircraft Code: {rec.IATACode} - Predicted Aircraft Code: {prediction.IATACode}, Confidence: {prediction.Confidence}");
                        }
                    }
                    else
                    {
                        incorrect++;
                        if (incorrect % 30 == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"FlightCode: {rec.FlightCode}, Aircraft Code: {rec.IATACode} - Predicted Aircraft Code: {prediction.IATACode}, Confidence: {prediction.Confidence}");
                        }
                    }
                }
            }
            var accuracy = (float)correct / (correct + incorrect);
            Console.ForegroundColor = defaultColor;
            Console.WriteLine($"Accuracy: {accuracy}");
            Console.WriteLine();
            return accuracy;
        }

    }
}
