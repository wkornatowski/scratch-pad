using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

public class PipeExample
{
    public static async Task WriteMemoryStreamToPipeWriterAsync(MemoryStream memoryStream, PipeWriter pipeWriter)
    {
        memoryStream.Position = 0;
        byte[] buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var writeBuffer = pipeWriter.GetMemory(bytesRead);
            buffer.AsSpan(0, bytesRead).CopyTo(writeBuffer);
            pipeWriter.Advance(bytesRead);

            // Flush the data to the reader.
            var result = await pipeWriter.FlushAsync();

            if (result.IsCompleted)
            {
                break;
            }
        }

        // Signal to the reader that we're done writing.
        await pipeWriter.CompleteAsync();
    }
}

// Example usage
var memoryStream = new MemoryStream();
// Assume memoryStream is already filled with data.

var pipe = new Pipe();
var pipeWriter = pipe.Writer;

await PipeExample.WriteMemoryStreamToPipeWriterAsync(memoryStream, pipeWriter);

// At this point, you can read from the pipe's reader elsewhere in your application.
