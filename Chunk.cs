using Google.Apis.Auth.OAuth2;
using System.Net.Http;
using System.Net.Http.Headers;

class GcpResumableUploader
{
    private readonly HttpClient _httpClient;
    private readonly string _bucketName;
    private readonly string _objectName;
    private readonly string _uploadSessionUri;

    public GcpResumableUploader(HttpClient httpClient, string bucketName, string objectName)
    {
        _httpClient = httpClient;
        _bucketName = bucketName;
        _objectName = objectName;
        _uploadSessionUri = InitializeResumableSession(bucketName, objectName).Result;
    }

    private async Task<string> InitializeResumableSession(string bucketName, string objectName)
    {
        var token = await GoogleCredential.GetApplicationDefault().UnderlyingCredential
            .GetAccessTokenForRequestAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var initRequest = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/upload/storage/v1/b/{bucketName}/o?uploadType=resumable&name={Uri.EscapeDataString(objectName)}");
        initRequest.Headers.Add("X-Upload-Content-Type", "application/octet-stream");

        var response = await _httpClient.SendAsync(initRequest);
        response.EnsureSuccessStatusCode();

        return response.Headers.Location.ToString();
    }

    public async Task UploadFromPipeReaderAsync(PipeReader reader)
    {
        const int chunkSize = 256 * 1024; // 256KB, adjust as needed
        long position = 0;

        while (true)
        {
            var readResult = await reader.ReadAsync();
            var buffer = readResult.Buffer;
            var endOfStream = readResult.IsCompleted;

            while (buffer.Length >= chunkSize || (endOfStream && buffer.Length > 0))
            {
                var chunk = buffer.Slice(0, Math.Min(chunkSize, buffer.Length));
                
                var content = new ByteArrayContent(chunk.ToArray());
                content.Headers.ContentRange = new ContentRangeHeaderValue(position, position + chunk.Length - 1);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var uploadResponse = await _httpClient.PutAsync(_uploadSessionUri, content);
                uploadResponse.EnsureSuccessStatusCode();

                position += chunk.Length;
                buffer = buffer.Slice(chunk.Length);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (endOfStream)
            {
                if (buffer.Length > 0)
                {
                    // Upload the final chunk if it's smaller than the chunk size
                    var content = new ByteArrayContent(buffer.ToArray());
                    content.Headers.ContentRange = new ContentRangeHeaderValue(position, position + buffer.Length - 1);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    var uploadResponse = await _httpClient.PutAsync(_uploadSessionUri, content);
                    uploadResponse.EnsureSuccessStatusCode();
                }

                break; // Exit the loop if we've reached the end of the stream
            }
        }

        // Finalize the upload by completing the reader
        reader.Complete();
    }
}
