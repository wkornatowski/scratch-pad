using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Upload;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class ResumableUploadStream : Stream
{
    private readonly Uri uploadUri;
    private readonly HttpClient httpClient;
    private long position = 0;

    public ResumableUploadStream(Uri uploadUri, HttpClient httpClient)
    {
        this.uploadUri = uploadUri ?? throw new ArgumentNullException(nameof(uploadUri));
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => position; set => throw new NotSupportedException(); }

    public override void Flush()
    {
        // This method is not relevant for our stream.
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var content = new ByteArrayContent(buffer, offset, count);
        content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(position, position + count - 1);
        var response = httpClient.PutAsync(uploadUri, content).Result;

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to upload chunk: {response.StatusCode}");
        }

        position += count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Optionally finalize the upload if necessary.
        }
        base.Dispose(disposing);
    }
}
