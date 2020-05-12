using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using nClam;
using Newtonsoft.Json.Linq;

namespace ClamAVTrigger
{
    public class FileVirusScanner
    {

        private readonly EventGridEvent blobCreatedEvent;
        private readonly StorageBlobCreatedEventData blobCreatedEventData;
        private readonly Stream blobStream;
        private readonly ILogger logger;

        private readonly string StorageAccount;
        private readonly string ContainerName;
        private readonly string BlobName;
        
        public FileVirusScanner(EventGridEvent blobCreatedEvent, Stream blobStream, ILogger logger)
        {
            this.blobCreatedEvent = blobCreatedEvent;
            this.blobCreatedEventData = ((JObject)blobCreatedEvent.Data).ToObject<StorageBlobCreatedEventData>();
            this.blobStream = blobStream;
            this.logger = logger;

            var (storageAccount, containerName, blobName) = GetBlobDetails(blobCreatedEvent, logger);

            this.StorageAccount = storageAccount;
            this.ContainerName = containerName;
            this.BlobName = blobName;

            logger.LogInformation($"Subject - {blobCreatedEvent.Subject}");
            logger.LogInformation($"Topic - {blobCreatedEvent.Topic}");
            logger.LogInformation($"Blob details {storageAccount} {containerName} {blobName}");

            logger.LogInformation($"Url: {blobCreatedEventData.Url}");
            logger.LogInformation($"Api operation: {blobCreatedEventData.Api}");
            logger.LogInformation($"ContentType: {blobCreatedEventData.ContentType}");
            logger.LogInformation($"Blob Stream Length: {blobStream.Length} bytes");
        }

        public async Task PerformScan()
        {
            var isInfected = await PerformClamAVScan();

            if (isInfected) await DeleteBlobFile();
        }

        private (string StorageAccount, string ContainerName, string BlobName) GetBlobDetails(EventGridEvent blobCreatedEvent, ILogger log)
        {
            var storageAccount = blobCreatedEvent.Topic.Split("/").Last();
            var subject = blobCreatedEvent.Subject;
            var subjectArray = subject.Split("/").ToArray();
            var containerName = subjectArray.ElementAtOrDefault(4);

            string toBeSearched = "blobs/";
            int idx = subject.IndexOf(toBeSearched);
            var blobName = subject.Substring(idx + toBeSearched.Length);

            return (storageAccount, containerName, blobName);
        }

        private async Task<long> TimedExecutionAsync(Func<Task> action)
        {
            var sw = new Stopwatch();
            sw.Start();
            await action.Invoke();
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        private async Task<bool> PerformClamAVScan()
        {
            var isInfected = false;

            var clamAVServiceURI = Environment.GetEnvironmentVariable("ClamAVServiceURI");

            var clam = new ClamClient(clamAVServiceURI, 3310);

            ClamScanResult scanResult = null;

            var timeTaken = await TimedExecutionAsync(async () =>
            {
                scanResult = await clam.SendAndScanFileAsync(blobStream.ReadAllBytes());
            });

            logger.LogInformation($"Time taken to perform virus scan {timeTaken} ms");

            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    logger.LogInformation("The file is clean!");
                    break;
                case ClamScanResults.VirusDetected:
                    logger.LogInformation("Virus Found!");
                    logger.LogInformation("Virus name: {0}", scanResult.InfectedFiles.First().VirusName);
                    isInfected = true;
                    break;
                case ClamScanResults.Error:
                    logger.LogInformation("Error scanning file: {0}", scanResult.RawResult);
                    break;
            }

            return isInfected;
        }

        private async Task DeleteBlobFile()
        {
            var storageAccountConnectionString = Environment.GetEnvironmentVariable(StorageAccount);
            var cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);

            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(ContainerName);
            var cloudBlockBlobRef = blobContainer.GetBlockBlobReference(BlobName);
            var deletionSuccess = await cloudBlockBlobRef.DeleteIfExistsAsync();
            logger.LogInformation($"File Deleted: {BlobName}, Success: {deletionSuccess}");
        }
    }
}
