using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

public class PipeExample
{
    public static async Task WriteMemoryStreamToPipeWriterAsync(MemoryStream memoryStream, PipeWriter pipeWriter)
    {
        // Reset the position of the MemoryStream to ensure we read from the beginning.
        memoryStream.Position = 0;

        // Allocate a buffer for reading. This size could be adjusted based on your needs.
        byte[] buffer = new byte[4096];
        int bytesRead;

        // Read from MemoryStream and write to PipeWriter in a loop until we've read all data.
        while ((bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            // Write the data to the PipeWriter.
            await pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead));
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
