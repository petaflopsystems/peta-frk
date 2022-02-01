using Microsoft.Extensions.Logging;
using Petaframework.Interfaces;

namespace Petaframework
{
    internal class PtfkMedia : IPtfkMedia
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
        public byte[] Bytes { get; set; }
        public string Path { get; set; }
        public string IsDeath { get; set; }
        public string EntityName { get; set; }
        public long EntityId { get; set; }
        public string EntityProperty { get; set; }

        public string ClassName => nameof(PtfkMedia);

        public string ExternalInfo { get; set; }

        public ILogger Logger => null;
    }
}
