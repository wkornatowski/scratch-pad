private async Task UploadFromPipeReaderAsync(PipeReader reader, string bucketName, string objectName, StorageClient storageClient)
{
    Uri sessionUri = await InitiateResumableUploadSessionAsync(bucketName, objectName, storageClient);
    long position = 0; // Tracks the position in the file

    while (true)
    {
        // Read from the PipeReader
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;
        var endOfStream = result.IsCompleted;

        // Process the buffer in chunks
        while (buffer.Length > 0)
        {
            // Determine the size of the current chunk
            var chunkLength = Math.Min(chunkSize, buffer.Length);
            var chunk = buffer.Slice(0, chunkLength);

            // Convert the chunk to a byte array (consider optimizing this for large buffers)
            byte[] chunkArray = chunk.ToArray();

            // Upload the chunk to GCS
            await UploadChunkAsync(sessionUri, chunkArray, position, position + chunkLength - 1, endOfStream && buffer.Length <= chunkLength);
            position += chunkLength;

            // Advance the buffer
            buffer = buffer.Slice(chunkLength);
        }

        // Advance the reader to the next piece of data
        reader.AdvanceTo(buffer.Start, buffer.End);

        if (endOfStream)
        {
            break; // Exit the loop if we've reached the end of the stream
        }
    }

    reader.Complete(); // Mark the PipeReader as complete
}


private async Task UploadChunkAsync(Uri sessionUri, byte[] chunkData, long rangeStart, long rangeEnd, bool isLastChunk)
{
    using (var httpClient = new HttpClient())
    {
        using (var content = new ByteArrayContent(chunkData))
        {
            // Set the content type to "application/octet-stream" or as appropriate for your file
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // Prepare the Content-Range header
            string contentRange = isLastChunk ? $"bytes {rangeStart}-{rangeEnd}/*" : $"bytes {rangeStart}-{rangeEnd}/{rangeEnd + 1}";
            httpClient.DefaultRequestHeaders.Add("Content-Range", contentRange);

            // Send the PUT request to the resumable session URI
            HttpResponseMessage response = await httpClient.PutAsync(sessionUri, content);

            if (!response.IsSuccessStatusCode)
            {
                // Handle unsuccessful upload attempt here
                throw new HttpRequestException($"Failed to upload chunk. Status: {response.StatusCode}");
            }

            // Optionally, handle the response to check for completion, get the uploaded object's details, etc.
        }
    }
}
