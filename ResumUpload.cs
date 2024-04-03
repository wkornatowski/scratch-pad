private async Task ReadPipeAndUploadToGcpAsync(PipeReader reader, string bucketName, string objectName)
{
    var storageClient = await StorageClient.CreateAsync();
    var uploadUri = await InitiateResumableUploadSessionAsync(storageClient, bucketName, objectName);

    const int chunkSize = 256 * 1024; // 256KB, GCS requires chunk sizes to be multiples of 256KB
    HttpClient httpClient = new HttpClient();
    long totalBytesUploaded = 0;

    while (true)
    {
        // Read from the pipe until we accumulate enough data to form a chunk
        var readResult = await reader.ReadAsync();
        var buffer = readResult.Buffer;
        var position = buffer.Start;

        // Check if there is enough data to form a chunk
        if (buffer.Length < chunkSize && !readResult.IsCompleted)
        {
            // Not enough data to form a chunk, and more data is expected
            reader.AdvanceTo(buffer.Start, buffer.End);
            continue;
        }

        // Process all full chunks available in the buffer
        while (buffer.Length >= chunkSize || (buffer.Length > 0 && readResult.IsCompleted))
        {
            var chunk = buffer.Slice(position, Math.Min(chunkSize, buffer.Length));
            
            // Prepare and perform the HTTP PUT request for the current chunk
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, uploadUri)
            {
                Content = new StreamContent(new ReadOnlyMemoryStream(chunk.ToArray()))
            };
            requestMessage.Headers.Add("Content-Range", $"bytes {totalBytesUploaded}-{totalBytesUploaded + chunk.Length - 1}/{(readResult.IsCompleted ? totalBytesUploaded + chunk.Length : "*")}");

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            totalBytesUploaded += chunk.Length;
            position = buffer.GetPosition(chunk.Length, position);

            if (readResult.IsCompleted && buffer.Length == chunk.Length)
            {
                // This was the last chunk
                break;
            }
        }

        reader.AdvanceTo(position);

        if (readResult.IsCompleted)
        {
            break;
        }
    }

    reader.Complete();
}
