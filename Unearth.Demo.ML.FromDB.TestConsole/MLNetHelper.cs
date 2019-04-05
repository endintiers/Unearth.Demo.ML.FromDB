using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.IO;
using System.Linq;
using Unearth.Demo.ML.FromDB.Common.Sql;
using Unearth.Demo.ML.FromDB.TestConsole.Models;

namespace Unearth.Demo.ML.FromDB.TestConsole
{
    public class MLNetHelper
    {
        public static ITransformer TrainModel(DbContextOptions<AirlinesContext> dbOptions,
                bool cacheData = false, int concurrency = 0, int nth = 1)
        {
            ITransformer trainedModel = null;

            // Create an ML.NET environment
            var mlContext = new MLContext(seed: 0, conc: concurrency);

            // Train from EF DBContext
            using (var airlinesModel = new AirlinesContext(dbOptions))
            {
                // Create an enumerable view of the DB training data
                var flightCodeTrainingData = airlinesModel.FlightCodes.Where(fc => fc.Id % nth == 0).AsEnumerable()
                    .Select(f => new FlightCodeFeatures()
                    {
                        FlightCode = f.FlightCode,
                        IATACode = f.Iatacode
                    });
                var trainingDataView = mlContext.Data.LoadFromEnumerable(flightCodeTrainingData);

                // Set the key column (IATACode), featurize the text FlightCode column (to a long) and add it to the features collection
                var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: DefaultColumnNames.Label, inputColumnName: nameof(FlightCodeFeatures.IATACode))
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "FlightCodeFeaturized", inputColumnName: nameof(FlightCodeFeatures.FlightCode)))
                .Append(mlContext.Transforms.Concatenate(outputColumnName: DefaultColumnNames.Features, "FlightCodeFeaturized"));

                if (cacheData)
                {
                    // Optionally cache the input (used if multiple passes required)
                    dataProcessPipeline.AppendCacheCheckpoint(mlContext);
                }

                // Define the trainer to be used
                IEstimator<ITransformer> trainer = null;
                trainer = mlContext.MulticlassClassification.Trainers.StochasticDualCoordinateAscent(DefaultColumnNames.Label, DefaultColumnNames.Features);

                // Create a training pipeline that adds the trainer to the data pipeline and maps prediction to a string in the output (default name)
                var trainingPipeline = dataProcessPipeline.Append(trainer)
                        .Append(mlContext.Transforms.Conversion.MapKeyToValue(DefaultColumnNames.PredictedLabel));

                // Do the actual training, reads the features and builds the model
                Console.WriteLine($"Starting training (concurrency {concurrency})");
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
            var flightCodePredictor = model.CreatePredictionEngine<FlightCodeFeatures, FlightCodePrediction>(mlContext);

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
