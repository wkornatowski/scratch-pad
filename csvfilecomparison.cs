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

            // First, compare the line counts
            if (await LineCountsMatchAsync(firstFilePath, secondFilePath))
            {
                Console.WriteLine("Line counts match. Proceeding with line-by-line comparison...");
                await CompareCsvFilesAsync(firstFilePath, secondFilePath);
            }
            else
            {
                Console.WriteLine("Line counts do not match. Files are different.");
            }
        }

        static async Task<bool> LineCountsMatchAsync(string firstFilePath, string secondFilePath)
        {
            var firstFileLineCount = await CountLinesAsync(firstFilePath);
            var secondFileLineCount = await CountLinesAsync(secondFilePath);

            return firstFileLineCount == secondFileLineCount;
        }

        static async Task<int> CountLinesAsync(string filePath)
        {
            int count = 0;
            using (var reader = new StreamReader(filePath))
            {
                while (await reader.ReadLineAsync() != null)
                {
                    count++;
                }
            }
            return count;
        }

        static async Task CompareCsvFilesAsync(string firstFilePath, string secondFilePath)
        {
            using var firstFileReader = new StreamReader(firstFilePath);
            using var secondFileReader = new StreamReader(secondFilePath);

            string firstLine, secondLine;
            int lineCount = 0;
            while ((firstLine = await firstFileReader.ReadLineAsync()) != null &&
                   (secondLine = await secondFileReader.ReadLineAsync()) != null)
            {
                lineCount++;
                if (firstLine != secondLine)
                {
                    Console.WriteLine($"Difference found at line {lineCount}:");
                    Console.WriteLine($"File 1: {firstLine}");
                    Console.WriteLine($"File 2: {secondLine}");
                }
            }
        }
    }
}
