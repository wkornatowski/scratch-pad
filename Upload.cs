private async Task UploadChunksToGcp(PipeReader reader, string bucketName, string baseObjectName, StorageClient storageClient)
{
    const int chunkSize = 2 * 1024 * 1024; // For example, 2MB chunks
    int partNumber = 0;

    while (true)
    {
        var result = await reader.ReadAsync();
        var buffer = result.Buffer;
        
        // Process the buffer in chunkSize blocks
        while (buffer.Length >= chunkSize)
        {
            partNumber++;
            var chunk = buffer.Slice(0, chunkSize);
            buffer = buffer.Slice(chunkSize);

            // Convert ReadOnlySequence<byte> chunk to a MemoryStream
            await using var chunkStream = new MemoryStream();
            foreach (var segment in chunk)
            {
                await chunkStream.WriteAsync(segment.ToArray(), 0, segment.Length);
            }
            chunkStream.Seek(0, SeekOrigin.Begin); // Reset stream position

            // Upload this chunk to GCP
            var objectName = $"{baseObjectName}_part_{partNumber:D5}";
            await storageClient.UploadObjectAsync(bucketName, objectName, null, chunkStream);

            Console.WriteLine($"Uploaded {objectName}");
        }

        // Indicate how much of the buffer has been consumed
        reader.AdvanceTo(buffer.Start, buffer.End);

        // Stop if there's no more data
        if (result.IsCompleted)
        {
            if (buffer.Length > 0)
            {
                // Handle any remaining bytes that didn't form a full chunk
                partNumber++;
                await using var remainingStream = new MemoryStream();
                foreach (var segment in buffer)
                {
                    await remainingStream.WriteAsync(segment.ToArray(), 0, segment.Length);
                }
                remainingStream.Seek(0, SeekOrigin.Begin); // Reset stream position
                
                var remainingObjectName = $"{baseObjectName}_part_{partNumber:D5}";
                await storageClient.UploadObjectAsync(bucketName, remainingObjectName, null, remainingStream);

                Console.WriteLine($"Uploaded {remainingObjectName}");
            }
            
            reader.AdvanceTo(buffer.End);
            break;
        }
    }
    // Mark the reader as complete
    reader.Complete();
}
