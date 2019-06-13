using System;
using MongoDB.Bson;

namespace GridFSExample
{
    public class BucketFileInfo
    {
        public ObjectId Id { get; set; }

        public DateTime UploadTime { get; private set; }

        public string FileName { get; set; }

        public long FileSize { get; set; }

        public BucketFileInfo(DateTime uploadTime)
        {
            UploadTime = uploadTime;
        }
    }
}
