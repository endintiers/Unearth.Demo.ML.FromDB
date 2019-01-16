using System;
using System.IO;
using System.Reflection;
using Unearth.Demo.ML.FromDB.MLNET8;

namespace Unearth.Demo.ML.FromDB.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var mdfFilePath = Path.Combine(GetAssemblyPath(), @"TrainingData\Airlines.mdf");
            var connectionString = $@"Data Source=localhost\SQLEXPRESS;AttachDbFilename={mdfFilePath};Integrated Security=True";

            var model = MLNETHelper8.TrainModel(connectionString, 2);

            //MLNETHelper8.TestModel(model);

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
