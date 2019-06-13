using System;
using System.IO;

namespace GridFSExample
{
    static class Program
    {
        static void Main(string[] args)
        {
            var fMgr = new BucketFileManager();
            Console.Write("File to upload:");
            var filepath = Console.ReadLine();
            try
            {
                fMgr.UploadFile(filepath);
            }
            catch
            {
                //
            }
            var begin = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0));
            Console.WriteLine($"Files in bucket (uploaded after {begin}):");
            var infos = fMgr.GetAllFiles(begin);       
            foreach(var info in infos)
            {
                Console.WriteLine($"filename:{info.FileName}, time:{info.UploadTime.ToLocalTime()}");
            }
           
            var filename = Path.GetFileName(filepath);
            Console.Write($"Hit <ENTER> to download file \'{filename}\'");
            Console.ReadLine();
            var bytes = fMgr.DownloadFile(filename);
            Console.WriteLine($"File downloaded, size = {bytes.Length}bytes");
            Console.ReadKey();
        }
    }
}
