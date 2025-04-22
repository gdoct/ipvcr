using System.IO.Abstractions;

namespace ipvcr.Tests
{
    public class MockedFileSystemStream : FileSystemStream
    {
        public MockedFileSystemStream(Stream stream, string path = "/mocked/path", bool isAsync = false)
            : base(stream, path ?? "/mocked/path", isAsync)
        {
        }
    }
}