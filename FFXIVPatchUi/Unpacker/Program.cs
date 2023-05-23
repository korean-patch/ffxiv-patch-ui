using System.IO.Compression;
using System.IO;
using System.Reflection;
using System;

namespace Unpacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string distribPath = Path.Combine(curDir, "distrib");
            string distribDir = Path.Combine(curDir, "distribOutput");
            if (Directory.Exists(distribDir)) Directory.Delete(distribDir, true);
            Directory.CreateDirectory(distribDir);

            if (File.Exists(Path.Combine(curDir, "000000.win32.dat1"))) File.Delete(Path.Combine(curDir, "000000.win32.dat1"));
            if (File.Exists(Path.Combine(curDir, "000000.win32.index"))) File.Delete(Path.Combine(curDir, "000000.win32.index"));
            if (File.Exists(Path.Combine(curDir, "0a0000.win32.dat1"))) File.Delete(Path.Combine(curDir, "0a0000.win32.dat1"));
            if (File.Exists(Path.Combine(curDir, "0a0000.win32.index"))) File.Delete(Path.Combine(curDir, "0a0000.win32.index"));

            if (!File.Exists(distribPath))
            {
                Console.WriteLine("distrib 파일을 발견하지 못했습니다.");
                Console.WriteLine("프로그램을 종료합니다.");

                return;
            }

            ZipFile.ExtractToDirectory(distribPath, distribDir);

            using (FileStream inStream = new FileStream(Path.Combine(distribDir, "000000win32dat1"), FileMode.Open))
            using (FileStream outStream = new FileStream(Path.Combine(curDir, "000000.win32.dat1"), FileMode.Create))
            using (GZipStream gzStream = new GZipStream(inStream, CompressionMode.Decompress))
            {
                gzStream.CopyTo(outStream);
            }

            using (FileStream inStream = new FileStream(Path.Combine(distribDir, "000000win32index"), FileMode.Open))
            using (FileStream outStream = new FileStream(Path.Combine(curDir, "000000.win32.index"), FileMode.Create))
            using (GZipStream gzStream = new GZipStream(inStream, CompressionMode.Decompress))
            {
                gzStream.CopyTo(outStream);
            }

            using (FileStream inStream = new FileStream(Path.Combine(distribDir, "0a0000win32dat1"), FileMode.Open))
            using (FileStream outStream = new FileStream(Path.Combine(curDir, "0a0000.win32.dat1"), FileMode.Create))
            using (GZipStream gzStream = new GZipStream(inStream, CompressionMode.Decompress))
            {
                gzStream.CopyTo(outStream);
            }

            using (FileStream inStream = new FileStream(Path.Combine(distribDir, "0a0000win32index"), FileMode.Open))
            using (FileStream outStream = new FileStream(Path.Combine(curDir, "0a0000.win32.index"), FileMode.Create))
            using (GZipStream gzStream = new GZipStream(inStream, CompressionMode.Decompress))
            {
                gzStream.CopyTo(outStream);
            }

            Directory.Delete(distribDir, true);
        }
    }
}
