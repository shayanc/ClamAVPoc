# Clam AntiVirus Poc

Proof of concept to test ClamAV functionality. 

## Getting Started

Start the ClamAV Container via this docker command

docker run -d -p 3310:3310 mkodockx/docker-clamav:alpine

More Information on the image here - 

https://hub.docker.com/r/mkodockx/docker-clamav/ 

https://github.com/mko-x/docker-clamav

ClamAV daemon as a Docker image. It builds with a current virus database and runs freshclam in the background constantly updating the virus signature database. clamd itself is listening on exposed port 3310.

## Sample implementation
An example implementation and sample EICAR files are provided in the `/ClaimAVScanTest` folder.
Once the ClamAV container is started, you can run the app with `dotnet run` to scan the files and view the results.

## ACI template for ClamAV container
A sample ACI template is provided to quickly setup the ClamAV service in Azure using ACI. See the folder `AzureContainerInstanceTemplate` for the template.

## Azure function blob trigger sample implemantion
A sample azure function implementaton which uses the EventGrid trigger on BlobCreated events is provided in the `/ClamAVAzureBlobTrigger` folder. It reacts to files added in the blobstorage and runs the scan. If the file is infected, it will delete the file from the blob storage. For the blob bindings to work and to delete the files, you will need to add the blob storage connection string in the `local.settings.json` file. You do not have to use the bindings if the blob is public. Instead you can download the file using WebClient to get the stream. 

More information here - [Azure Blob storage bindings for Azure Functions overview](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob)

To link the storage account to the function you will need to go to 

Azure portal -> storage account -> events -> add event subscribtion -> select blob created event -> choose azure function as your trigger

If the events tab is missing, it may mean your storage account is on gen v1 and you will need to upgrade it to gen v2 from the configuration tab. 

**Create new event subscription**

![Create new event subscription](https://github.com/shayanc/ClamAVPoc/blob/master/_Images/NewSubscription.png)

**Select event type**

![test](https://github.com/shayanc/ClamAVPoc/blob/master/_Images/CreateEvent.png)

**Link the azure function**

![test](https://github.com/shayanc/ClamAVPoc/blob/master/_Images/ChooseFunction.png)
