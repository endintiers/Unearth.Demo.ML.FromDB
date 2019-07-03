using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unearth.Demo.ML.FromDB.Common.Sql;
using Unearth.Demo.ML.FromDB.TestConsole.Models;

namespace Unearth.Demo.ML.FromDB.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            IConfigurationRoot configuration = builder.Build();

            // Use an in-memory DB
            var dbOptions = new DbContextOptionsBuilder<AirlinesContext>()
                .UseInMemoryDatabase(databaseName: "FlightCodes")
                .Options;

            LoadAirlinesData(dbOptions);

            ITransformer model = null;
            try
            {
                model = MLNetHelper.TrainModel(dbOptions, cacheData: true, nth: 1);
            }
            catch (AggregateException)
            {
                // This happens when multiple threads attempt to read from the EF DBContext
                Console.WriteLine("Got AggregateException while training");
            }

            if (model != null)
                MLNetHelper.TestModel(model);

            Console.WriteLine("Finished - press enter to exit");
            Console.ReadLine();
        }

        /// <summary>
        /// Creates an in-memory EF Core Database and loads it with airline data
        /// </summary>
        /// <param name="options"></param>
        private static void LoadAirlinesData(DbContextOptions<AirlinesContext> options)
        {
            // Load data into the DB
            using (var airlinesModel = new AirlinesContext(options))
            {
                // Key for fake Ids
                int key = 0;

                // Load the FlightCode Data from csv
                using (TextReader reader = new StreamReader(@"TrainingData\ManyFlightCodes.csv"))
                {
                    var csvReader = new CsvReader(reader);
                    var flightCodes = csvReader.GetRecords<FlightCodeFeatures>();
                    airlinesModel.FlightCodes.AddRange(flightCodes.Select(f =>
                    {
                        var fc = new FlightCodes();
                        fc.Id = ++key;
                        fc.FlightCode = f.FlightCode;
                        fc.Iatacode = f.IATACode;
                        return fc;
                    }));
                    airlinesModel.SaveChanges();
                }
            }
        }

        private static string GetAssemblyPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string result = Uri.UnescapeDataString(uri.Path);
            result = Path.GetDirectoryName(result);
            return result;
        }
    }

}
