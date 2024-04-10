using System;
using System.IO;
using System.Threading.Tasks;

namespace CsvFileComparer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Paths to the local CSV files
            string firstFilePath = "path/to/your/first-large-file.csv";
            string secondFilePath = "path/to/your/second-large-file.csv";

            await CompareCsvFilesAsync(firstFilePath, secondFilePath);
        }

        static async Task CompareCsvFilesAsync(string firstFilePath, string secondFilePath)
        {
            // Open the files for reading
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
        }
    }
}
