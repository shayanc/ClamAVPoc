using nClam;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClaimAVTest
{
    class Program
    {
        static readonly string serverName = "localhost";
        static readonly int serverPort = 3310;

        public static async Task<long> TimedExecutionAsync(Func<Task> action)
        {
            var sw = new Stopwatch();
            sw.Start();
            await action.Invoke();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public static async Task<(ClamScanResult ScanResult, string Filename, long TimeTaken, long FileSize)> ProcessFile(ClamClient clam, string filePath)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            ClamScanResult scanResult = null;
            var time = await TimedExecutionAsync(async () =>
            {
                scanResult = await clam.SendAndScanFileAsync(fileBytes);
            });
            return (scanResult, filePath, time, fileBytes.Length);
        }

        static async Task Main(string[] args)
        {
            // Create ClaimAV client
            var clam = new ClamClient(serverName, serverPort);

            var targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "files");

            Console.WriteLine("Working directory: {0}\n\n", targetDirectory);

            var processFileTasks =
                Directory
                    .GetFiles(targetDirectory)
                    .Select(filePath => ProcessFile(clam, filePath))
                    .ToList();

            while (processFileTasks.Count > 0)
            {
                var scanResultTask = await Task.WhenAny(processFileTasks);

                processFileTasks.Remove(scanResultTask);
                
                var scanResponse = await scanResultTask;

                var (scanResult, fileName, time, size) = scanResponse;

                Console.WriteLine("File Processed - {0}", Path.GetFileName(fileName));
                switch (scanResult.Result)
                {
                    case ClamScanResults.Clean:
                        Console.WriteLine("The file is clean!");
                        break;
                    case ClamScanResults.VirusDetected:
                        Console.WriteLine("Virus Found!");
                        Console.WriteLine("Virus name: {0}", scanResult.InfectedFiles.First().VirusName);
                        break;
                    case ClamScanResults.Error:
                        Console.WriteLine("Error scanning file: {0}", scanResult.RawResult);
                        break;
                }
                Console.WriteLine($"Time to execute : {time} ms");
                Console.WriteLine($"File size : {size} bytes");
                Console.WriteLine("\n".PadLeft(48, '-'));
            }
            
            Console.WriteLine("Done...");
            Console.ReadLine();
        }
    }
}
