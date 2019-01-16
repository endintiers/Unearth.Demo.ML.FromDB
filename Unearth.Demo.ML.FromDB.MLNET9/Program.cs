using System;
using System.IO;
using System.Reflection;

namespace Unearth.Demo.ML.FromDB.MLNET9
{
    class Program
    {
        static void Main(string[] args)
        {
            var mdfFilePath = Path.Combine(GetAssemblyPath(), @"TrainingData\Airlines.mdf");
            var connectionString = $@"Data Source=localhost\SQLEXPRESS;AttachDbFilename={mdfFilePath};Integrated Security=True";

            var model = MLNETHelper9.TrainModel(connectionString, 2, 1);
            try
            {
                model = MLNETHelper9.TrainModel(connectionString, 2, 2);
            }
            catch (AggregateException)
            {
                Console.WriteLine("Got AggregateException while training multi-threaded");
            }
            //MLNETHelper9.TestModel(model);

            Console.WriteLine("Finished - press enter to exit");
            Console.ReadLine();
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
