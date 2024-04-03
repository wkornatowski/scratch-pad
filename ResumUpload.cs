private async Task<Uri> InitiateResumableUploadSessionAsync(string bucketName, string objectName, StorageClient storageClient)
{
    var insertRequest = storageClient.Service.Objects.Insert(
        new Google.Apis.Storage.v1.Data.Object() { Name = objectName, Bucket = bucketName }, 
        bucketName, 
        new MemoryStream(), // Placeholder stream, the actual data stream is managed separately
        "application/octet-stream"
    );
    insertRequest.PredefinedAcl = "bucketOwnerFullControl"; // Adjust the ACL as needed
    insertRequest.UploadType = "resumable";

    var sessionUri = await insertRequest.InitiateSessionAsync();

    return sessionUri;
}


private async Task UploadChunksResumableAsync(PipeReader reader, string bucketName, string objectName, StorageClient storageClient)
{
    const int chunkSize = 256 * 1024; // GCS resumable upload chunk size must be a multiple of 256 KB
    var sessionUri = await InitiateResumableUploadSessionAsync(bucketName, objectName, storageClient);

    long totalBytesUploaded = 0;
    var buffer = new byte[chunkSize];
    var httpClient = new HttpClient();

    while (true)
    {
        var readResult = await reader.ReadAsync();
        var sequence = readResult.Buffer;
        var position = sequence.Start;

        while (sequence.TryGet(ref position, out var memory))
        {
            var bytesRead = memory.Length;
            if (bytesRead == 0) break;

            memory.Span.CopyTo(buffer);
            using (var content = new ByteArrayContent(buffer, 0, bytesRead))
            {
                content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(totalBytesUploaded, totalBytesUploaded + bytesRead - 1);
                content.Headers.ContentLength = bytesRead;
                var response = await httpClient.PutAsync(sessionUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to upload chunk: {response.ReasonPhrase}");
                }

                totalBytesUploaded += bytesRead;
            }
        }

        reader.AdvanceTo(sequence.End);

        if (readResult.IsCompleted)
        {
            break;
        }
    }

    // Finalize the upload if necessary by sending a final PUT request with zero bytes of data
    // to signify the end of the upload.
    using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, sessionUri))
    {
        requestMessage.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(totalBytesUploaded, totalBytesUploaded) { IsLastBytePresent = true };
        var finalResponse = await httpClient.SendAsync(requestMessage);

        if (!finalResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to finalize upload: {finalResponse.ReasonPhrase}");
        }
    }

    reader.Complete();
}
