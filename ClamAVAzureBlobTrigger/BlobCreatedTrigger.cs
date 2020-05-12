using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace ClamAVTrigger
{
    public static class BlobCreatedTrigger
    {       
        // Note - the blob binding is not essential, if the blob is public you can also 
        // get stream through downloading the blob using the provided url with WebClient

        [FunctionName("TestEnvScanFile")]
        public static async Task RunScanForTestEnv(
            [EventGridTrigger]EventGridEvent blobCreatedEvent,
            [Blob("{data.url}", FileAccess.Read, Connection = "testaccount")] Stream blobStream,
            ILogger logger)
        {
            try
            {
                var fileScanner = new FileVirusScanner(blobCreatedEvent, blobStream, logger);
                await fileScanner.PerformScan();
            }
            catch (Exception e)
            {
                logger.LogInformation($"Error: {e.GetBaseException()} ");
                logger.LogInformation($"Error: {e.Message}");
                logger.LogInformation($"Error: {e.StackTrace} ");
                throw e;
            }
        }

        [FunctionName("ProdEnvScanFile")]
        public static async Task RunScanForProdEnv(
            [EventGridTrigger]EventGridEvent blobCreatedEvent,
            [Blob("{data.url}", FileAccess.Read, Connection = "prodaccount")] Stream blobStream,
            ILogger logger)
        {
            try
            {
                var fileScanner = new FileVirusScanner(blobCreatedEvent, blobStream, logger);
                await fileScanner.PerformScan();
            }
            catch (Exception e)
            {
                logger.LogInformation($"Error: {e.GetBaseException()} ");
                logger.LogInformation($"Error: {e.Message}");
                logger.LogInformation($"Error: {e.StackTrace} ");
                throw e;
            }
        }
    }
}
