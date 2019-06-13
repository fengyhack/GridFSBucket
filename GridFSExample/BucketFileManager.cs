using System.Linq;
using System.IO;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;

namespace GridFSExample
{
    public class BucketFileManager
    {
        private readonly string fileInfoDbName = "FileInfo";
        private readonly string fileBucketDbName = "FileBucket";
        private readonly string bucket = "DEFAULT_BUCKET";

        private readonly GridFSBucket fsBucket = null;

        private readonly IMongoDatabase fileInfoDB = null;

        public BucketFileManager(string connStr = "mongodb://localhost:27017")
        {
            var client = new MongoClient(connStr);
            fileInfoDB = client.GetDatabase(fileInfoDbName);
            var db = client.GetDatabase(fileBucketDbName);
            fsBucket = new GridFSBucket(db, new GridFSBucketOptions { BucketName = bucket});
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public bool HasFile(string filename)
        {
            var collection = fileInfoDB.GetCollection<BucketFileInfo>(fileInfoDbName);
            return collection.Find((info) => info.FileName.Equals(filename)).Any();
        }

        /// <summary>
        /// 查找文件，上传时间范围为[begin,end)，其中end默认值为DateTime.Now
        /// </summary>
        /// <param name="begin">起始时间</param>
        /// <param name="end">结束时间，设为null表示使用DateTime.Now</param>
        public List<BucketFileInfo> GetAllFiles(DateTime begin, DateTime? end = null)
        {
            var end_ = end ?? DateTime.Now;
            var collection = fileInfoDB.GetCollection<BucketFileInfo>(fileInfoDbName);
            return collection.Find(info => info.UploadTime >= begin && info.UploadTime < end_).ToList();
        }

        /// <summary>
        /// 存储文件到FileBucket数据库
        /// </summary>
        /// <param name="filepath">本地文件</param>
        /// <param name="savename">保存到Bucket中的文件名</param>
        public void UploadFile(string filepath, string savename = null)
        {
            var fpath = filepath.Trim('"');
            var bytes = File.ReadAllBytes(fpath);
            if(string.IsNullOrEmpty(savename))
            {
                savename = Path.GetFileName(fpath);
            }
            var collection = fileInfoDB.GetCollection<BucketFileInfo>(fileInfoDbName);
            if (collection.Find(info => info.FileName.Equals(savename)).Any())
            {
                throw new GridFSException($"\'{savename}\' already exists");
            }

            var id = fsBucket.UploadFromBytes(savename, bytes);

            // 更新到FilInfo数据库
            var fileInfo = new BucketFileInfo(DateTime.UtcNow)
            {
                Id = id,
                FileName = savename,
                FileSize = bytes.Length,
            };
            
            collection.InsertOne(fileInfo);
        }

        /// <summary>
        /// 从FileBucket下载文件
        /// </summary>
        public byte[] DownloadFile(string filename)
        {
            // 从FileInfo查找文件信息
            var collection = fileInfoDB.GetCollection<BucketFileInfo>(fileInfoDbName);
            var results = collection.Find(fileInfo => fileInfo.FileName.Equals(filename));

            if (results.Any())
            {
                var id = results.FirstOrDefault().Id;
                return fsBucket.DownloadAsBytes(id);
            }
            else
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filename"></param>
        public bool DeleteFile(string filename)
        {
            // 从FileInfo查找文件信息
            var collection = fileInfoDB.GetCollection<BucketFileInfo>(fileInfoDbName);
            var results = collection.Find(fileInfo => fileInfo.FileName.Equals(filename));

            if (results.Any())
            {
                if(results.CountDocuments()>1)
                {
                    throw new GridFSException($"more than one file found with name {filename}");
                }

                var id = results.First().Id;
                collection.DeleteOne(info => info.Id == id);
                fsBucket.Delete(id);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
