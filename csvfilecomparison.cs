using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CsvFileComparer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string projectId = "your-gcp-project-id";
            string bucketName = "your-bucket-name";
            string firstFileName = "first-large-file.csv";
            string secondFileName = "second-large-file.csv";

            var storageClient = await StorageClient.CreateAsync();
            await CompareCsvFilesAsync(storageClient, projectId, bucketName, firstFileName, secondFileName);
        }

        static async Task CompareCsvFilesAsync(StorageClient storageClient, string projectId, string bucketName, string firstFileName, string secondFileName)
        {
            var firstFilePath = Path.GetTempFileName();
            var secondFilePath = Path.GetTempFileName();

            // Download the files to local temp files
            await storageClient.DownloadObjectAsync(bucketName, firstFileName, File.OpenWrite(firstFilePath));
            await storageClient.DownloadObjectAsync(bucketName, secondFileName, File.OpenWrite(secondFilePath));

            // Open the temp files for reading
            using var firstFileReader = new StreamReader(firstFilePath);
            using var secondFileReader = new StreamReader(secondFilePath);

            string firstLine, secondLine;
            int lineCount = 0;
            while ((firstLine = await firstFileReader.ReadLineAsync()) != null &&
                   (secondLine = await secondFileReader.ReadLineAsync()) != null)
            {
                lineCount++;
                // Compare the lines
                if (firstLine != secondLine)
                {
                    Console.WriteLine($"Difference found at line {lineCount}:");
                    Console.WriteLine($"File 1: {firstLine}");
                    Console.WriteLine($"File 2: {secondLine}");
                    // You might want to break here or handle differences accordingly
                }
            }

            // Clean up temp files
            File.Delete(firstFilePath);
            File.Delete(secondFilePath);
        }
    }
}
