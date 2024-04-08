using System;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var pipe = new Pipe();

        // Task to write to the pipe
        var writeTask = WriteAsync(pipe.Writer);

        // Await the completion of the writing task
        await writeTask;

        // Read the pipe into a MemoryStream
        using (MemoryStream memoryStream = new MemoryStream())
        {
            await CopyToMemoryStreamAsync(pipe.Reader, memoryStream);

            // Make sure to rewind the MemoryStream before reading it
            memoryStream.Position = 0;

            // Now you can use the MemoryStream with ZipArchive
            using (ZipArchive zip = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                // Example: List the contents of the zip
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    Console.WriteLine(entry.FullName);
                }
            }
        }
    }

    static async Task WriteAsync(PipeWriter writer)
    {
        // Example: Writing a zip file's bytes
        // In a real scenario, ensure the data written here is a valid zip file
        var zipFilePath = "path_to_your_zip_file.zip";
        using (FileStream zipStream = File.OpenRead(zipFilePath))
        {
            await zipStream.CopyToAsync(writer);
        }
        writer.Complete();
    }

    static async Task CopyToMemoryStreamAsync(PipeReader reader, MemoryStream memoryStream)
    {
        while (true)
        {
            ReadResult result = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = result.Buffer;

            foreach (var segment in buffer)
            {
                await memoryStream.WriteAsync(segment.ToArray(), 0, segment.Length);
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        reader.Complete();
    }
}
