using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace calchash
{
    class Program
    {
        // String for message concerning execution.
        static string message;

        // Accumulates size of processed files (in bytes).
        static double totalFileSize = 0;

        // Lock object for totalFileSize.
        static object locker = new object();

        // List of lines to write to an output file.
        static List<string> resultLines = new List<string>();

        // Recursively calls itself on all subdirectories and calls
        // ProcessFile for all files in it. Parallel.ForEach provides
        // parallelism.
        public static void ProcessDirectory(string dirPath, SHA256 sha)
        {
            try
            {
                string[] filePathArr = Directory.GetFiles(dirPath);
                string[] subdirPathArr = Directory.GetDirectories(dirPath);
                Parallel.ForEach(filePathArr, path => ProcessFile(path, sha));
                Parallel.ForEach(subdirPathArr, path => ProcessDirectory(path, sha));
            }
            catch (AggregateException ex)
            // In case if access to path is denied.
            when (ex.InnerException.GetType()
            .IsAssignableFrom(typeof(UnauthorizedAccessException)))
            {
                message = "Specified directory contained files or subdirectories " +
                    "with limited access which haven't been processed";
                return;
            }
        }

        // Calculates hash of a file; adds formatted string to the list;
        // adds processed file size to the accumulator.
        public static void ProcessFile(string filePath, SHA256 sha)
        {
            string hashStr = GetFileHashString(OpenReadFile(filePath), sha);
            hashStr = $"{hashStr} {filePath}";
            resultLines.Add(hashStr);
            lock (locker) // locking the critical section below
            {
                totalFileSize += new FileInfo(filePath).Length;
            }
        }

        // Opens an existing file for read access.
        // Returns FileStream of that file.
        static FileStream OpenReadFile(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read)
            {
                Position = 0
            };
        }

        // Calculates sha-256 hash of a file.
        // Returns string representation of a hash value.
        static string GetFileHashString(FileStream file, SHA256 sha)
        {
            byte[] hashValue = sha.ComputeHash(file);
            return BitConverter.ToString(hashValue).Replace("-", String.Empty);
        }

        static void Main(string[] args)
        {
            // Expecting 2 arguments. If not provided, print usage instead.
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: calchash.exe input_directory_name ouptut_file_name");
                return;
            }

            // Path to directory to process.
            string inputDirName = args[0];

            // Path to file for result.
            string outputFileName = args[1];

            FileStream file;
            try
            {
                file = File.Open(outputFileName, FileMode.Create);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"Invalid path to file: {outputFileName}");
                return;
            }

            file.Close();

            try
            {
                SHA256 sha = SHA256.Create();
                ProcessDirectory(inputDirName, sha);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine($"No such directory: {inputDirName}");
                return;
            }

            // Write all lines to the resulting file.
            File.AppendAllLines(outputFileName, resultLines);

            // Convert size to MB.
            totalFileSize = totalFileSize / (1024 * 1024);

            // Get the total processor time for this process.
            double seconds = Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds;
            string footer = $"Performance: {totalFileSize / seconds: 0.00} MB/s (by CPU time) ";
            File.AppendAllText(outputFileName, footer);

            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine(message);
            }
        }
    }
}
