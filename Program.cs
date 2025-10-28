using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Timers;

class Program
{
    static System.Timers.Timer syncTimer;
    static string logsPath = "";

    private static Dictionary<FileInfo, byte[]> sourceFileHashes = new Dictionary<FileInfo, byte[]>();
    //private static Dictionary<FileInfo, byte[]> destFileHashes = new Dictionary<FileInfo, byte[]>();
    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        try
        {
            DirectoryInfo dir = new(sourceDir);
            DirectoryInfo destDir = new(destinationDir);

            Log(logsPath, $"Attempting to create directory at: {destinationDir}");
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                string sourceFilePath = Path.Combine(sourceDir, file.Name);

                byte[] sourceHash = GetChecksum(sourceFilePath);
                sourceFileHashes[file] = sourceHash;

                bool needsCopy = true;

                if (File.Exists(targetFilePath))
                {
                    byte[] destHash = GetChecksum(targetFilePath);
                    needsCopy = !sourceHash.SequenceEqual(destHash);
                }

                if (needsCopy)
                {
                    Log(logsPath, $"Copying {file.Name}");
                    file.CopyTo(targetFilePath, true);
                }
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
        catch (FileNotFoundException)
        {
            Log(logsPath, "The file or directory cannot be found.");
        }
        catch (DirectoryNotFoundException)
        {
            Log(logsPath, "The file or directory cannot be found.");
        }
        catch (DriveNotFoundException)
        {
            Log(logsPath, "The drive specified in 'path' is invalid.");
        }
        catch (PathTooLongException)
        {
            Log(logsPath, "'path' exceeds the maximum supported path length.");
        }
        catch (UnauthorizedAccessException)
        {
            Log(logsPath, "You do not have permission to create this file.");
        }
        catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
        {
            Log(logsPath, "There is a sharing violation.");
        }
        catch (IOException e) when ((e.HResult & 0x0000FFFF) == 80)
        {
            Log(logsPath, "The file already exists.");
        }
        catch (IOException e)
        {
            Log(logsPath, $"An exception occurred:\nError code: " +
                              $"{e.HResult & 0x0000FFFF}\nMessage: {e.Message}");
        }

    }

    static void RemoveFromDirectory(string destinationDir, string baseSourceDir, string rootDestinationDir)
    {
        DirectoryInfo destDir = new(destinationDir);

        HashSet<string> sourcePaths = new HashSet<string>(
            sourceFileHashes.Keys.Select(f => Path.GetFullPath(f.FullName)),
            StringComparer.OrdinalIgnoreCase
        );

        foreach (FileInfo file in destDir.GetFiles())
        {
            string relativePath = Path.GetRelativePath(rootDestinationDir, file.FullName);
            string sourceFilePath = Path.Combine(baseSourceDir, relativePath);

            if (!sourcePaths.Contains(Path.GetFullPath(sourceFilePath)))
            {
                Log(logsPath, $"Deleting file: {file.FullName} (not in source)");
                file.Delete();
            }
        }

        foreach (DirectoryInfo subDir in destDir.GetDirectories())
        {
            string relativeDir = Path.GetRelativePath(rootDestinationDir, subDir.FullName);
            string sourceDirPath = Path.Combine(baseSourceDir, relativeDir);

            if (!Directory.Exists(sourceDirPath))
            {
                Log(logsPath, $"Deleting directory: {subDir.FullName} (not in source)");
                subDir.Delete(true);
            }
            else
            {
                RemoveFromDirectory(subDir.FullName, baseSourceDir, rootDestinationDir);
            }
        }
    }


    //static void GetAllDirectoriesFromDestination(string destinationDir)
    //{
    //    DirectoryInfo dir = new(destinationDir);
    //    DirectoryInfo[] dirs = dir.GetDirectories();
    //    foreach (DirectoryInfo subDir in dirs)
    //    {
    //        string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
    //        destinationDir.Add(dir.Name);
    //        GetAllDirectoriesFromDestination(newDestinationDir);
    //    }
    //}

    static void GetAllFilesFromSource(string sourceDir)
    {
        DirectoryInfo dir = new(sourceDir);
        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (FileInfo file in dir.GetFiles())
        {
            string sourceFilePath = Path.Combine(sourceDir, file.Name);
            sourceFileHashes[file] = GetChecksum(sourceFilePath);
        }
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(sourceDir, subDir.Name);
            GetAllFilesFromSource(newDestinationDir);
        }
    }

    static byte[] GetChecksum(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                //Console.WriteLine($"MD5 checksum of {filePath}: {BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}");
                return hash;
            }
        }
    }

    static void Log(string logsPath, string message)
    {
        using (StreamWriter sw = File.AppendText(logsPath))
        {
            Console.WriteLine(message);
            sw.WriteLine($"{DateTime.Now}: {message}");
        }
    }

    static string NormalizeLogPath(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentException("Log path cannot be empty.");

        if (Directory.Exists(inputPath) ||
            inputPath.EndsWith("\\") ||
            inputPath.EndsWith("/") ||
            string.IsNullOrEmpty(Path.GetExtension(inputPath)))
        {
            Directory.CreateDirectory(inputPath); 
            string logFile = Path.Combine(inputPath, "data.log");
            return logFile;
        }
        else
        {
            string directory = Path.GetDirectoryName(inputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return inputPath;
        }
    }

    static void Main()
    {
        Console.Write("Set folder source path: ");
        string source = Console.ReadLine();
        Console.Write("Set folder destination path: ");
        string destination = Console.ReadLine();
        Console.Write("Set log file path: ");
        string logsInput = Console.ReadLine();
        Console.Write("Set timer (in seconds): ");
        string timer = Console.ReadLine();

        logsPath = NormalizeLogPath(logsInput);

        string baseSourceDir = Path.GetFullPath(source);

        if (!double.TryParse(timer, out double intervalSeconds))
        {
            Log(logsPath, "Invalid timer value, using default 10 seconds.");
            intervalSeconds = 10;
        }

        bool isSyncing = false;

        syncTimer = new System.Timers.Timer(intervalSeconds * 1000);
        syncTimer.Elapsed += (sender, e) =>
        {
            if (isSyncing)
            {
                Log(logsPath, "Sync is already in progress, skipping this interval.");
                return;
            }

            isSyncing = true;

            try
            {
                Log(logsPath, $"Synchronizing folders at {DateTime.Now}...");

                sourceFileHashes.Clear();
                GetAllFilesFromSource(source);
                CopyDirectory(source, destination, true);
                RemoveFromDirectory(destination, baseSourceDir, destination);

                Log(logsPath, "Sync completed.");
            }
            catch (Exception ex)
            {
                Log(logsPath, $"An error occurred: {ex.Message}");
            }
            finally
            {
                isSyncing = false;
            }
        };

        syncTimer.AutoReset = true;
        syncTimer.Enabled = true;

        Console.WriteLine("Press Enter to stop...");
        Console.ReadLine();
        Log(logsPath, "Program stopped!");
    }

}

