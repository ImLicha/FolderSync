using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

class Program
{
    private static Dictionary<FileInfo, byte[]> sourceFileHashes = new Dictionary<FileInfo, byte[]>();
    private static Dictionary<FileInfo, byte[]> destFileHashes = new Dictionary<FileInfo, byte[]>();
    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        

        try
        {
            // Get information about the source directory
            DirectoryInfo dir = new(sourceDir);

            DirectoryInfo destDir = new(destinationDir);

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);
            
            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                string sourceFilePath = Path.Combine(sourceDir, file.Name);
                
                Console.WriteLine($"Copying {file.FullName} to {targetFilePath}");
                
                sourceFileHashes[file] = GetChecksum(sourceFilePath);
                if (!destFileHashes.ContainsKey(file) || sourceFileHashes[file] != destFileHashes[file])
                {
                    file.CopyTo(targetFilePath, true);
                    destFileHashes[file] = GetChecksum(targetFilePath);
                    
                }
                
                

                /*using (var md5 = MD5.Create())
                {
                    
                    using (var stream = File.OpenRead(sourceFilePath))
                    {
                        sourceFileHashes[file.Name] = md5.ComputeHash(stream);
                        sourceHash = sourceFileHashes[file.Name];
                    }
                    using (var stream = File.OpenRead(targetFilePath))
                    {
                        destFileHashes[file.Name] = md5.ComputeHash(stream);
                        destHash = destFileHashes[file.Name];
                    }
                }*/
                //Console.WriteLine($"{sourceFileHashes.Keys}");
                //Console.WriteLine($"MD5 checksum of {targetFilePath}: {BitConverter.ToString(sourceHash).Replace("-", "").ToLowerInvariant()}");
                //Console.WriteLine($"MD5 checksum of {sourceDir + file.Name}: {BitConverter.ToString(destHash).Replace("-", "").ToLowerInvariant()}");
            }
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("The file or directory cannot be found.");
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("The file or directory cannot be found.");
        }
        catch (DriveNotFoundException)
        {
            Console.WriteLine("The drive specified in 'path' is invalid.");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine("'path' exceeds the maximum supported path length.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("You do not have permission to create this file.");
        }
        catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
        {
            Console.WriteLine("There is a sharing violation.");
        }
        catch (IOException e) when ((e.HResult & 0x0000FFFF) == 80)
        {
            //TODO: Instead of a message, check file contents and skip files if they are identical. Use MD5 checksum for file comparison.
            Console.WriteLine("The file already exists.");
            //Console.WriteLine($"{sourceFileHashes.Keys}");
            foreach (var key in sourceFileHashes.Keys)
            {
                Console.WriteLine($"Checking file in source {key}...");
            }
            foreach (var key in sourceFileHashes.Keys)
            {
                Console.WriteLine($"Checking file in source {key}...");
            }
            //Console.WriteLine($"MD5 checksum of file in source: {BitConverter.ToString(sourceFileHashes["test"]).Replace("-", "").ToLowerInvariant()}");
            //Console.WriteLine($"MD5 checksum of file in destination: {BitConverter.ToString(sourceFileHashes["test"]).Replace("-", "").ToLowerInvariant()}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"An exception occurred:\nError code: " +
                              $"{e.HResult & 0x0000FFFF}\nMessage: {e.Message}");
        }

    }

    static void RemoveFromDirectionary()
    {
        HashSet<string> sourceNames = new HashSet<string>(
            sourceFileHashes.Keys.Select(f => f.Name),
            StringComparer.OrdinalIgnoreCase);
        
        List<FileInfo> missingInSource = destFileHashes.Keys
            .Where(f => !sourceNames.Contains(f.Name))
            .ToList();

        foreach (FileInfo file in missingInSource)
        {
            Console.WriteLine($"Deleting: {file.Name} as it's not present in source file");
            file.Delete();
            destFileHashes.Remove(file);
        }
    }

    static void GetAllFilesFromDestination(string destinationDir)
    {
        DirectoryInfo dir = new(destinationDir);
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            destFileHashes[file] = GetChecksum(targetFilePath);
        }
    }

    static void GetAllFilesFromSource(string sourceDir)
    {
        DirectoryInfo dir = new(sourceDir);
        foreach (FileInfo file in dir.GetFiles())
        {
            string sourceFilePath = Path.Combine(sourceDir, file.Name);
            sourceFileHashes[file] = GetChecksum(sourceFilePath);
        }
    }

    void CopyToDestination(FileInfo file, string targetFilePath)
    {
        file.CopyTo(targetFilePath, true);
        destFileHashes[file] = GetChecksum(targetFilePath);
    }
    static byte[] GetChecksum(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                Console.WriteLine($"MD5 checksum of {filePath}: {BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}");
                return hash;
            }
        }
    }

    void Update()
    {
        foreach (var item in sourceFileHashes)
        {
            if (!destFileHashes.ContainsKey(item.Key))
            {
                // Copy file to destination
            }
            else
            {
                // Compare hashes
                if (item.Value != destFileHashes[item.Key])
                {
                    // Copy file to destination
                }
            }
        }
    }

    static void Main()
    {
        while(true)
        {
            try
            {
                Console.Write("Set folder source path: ");
                string source = Console.ReadLine();
                Console.Write("Set folder destination path: ");
                string destination = Console.ReadLine();
                GetAllFilesFromDestination(destination);
                GetAllFilesFromSource(source);
                CopyDirectory(source, destination, true);
                Console.WriteLine("Removing files from destination that do not exist in source...");
                RemoveFromDirectionary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        
    }
}
//CopyDirectory(@".\", @".\copytest", true);    

