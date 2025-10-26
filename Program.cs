using System.IO;
using System.Security.Cryptography;

class Program
{
    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        try
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
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
            //TODO: Instead of a message, check file contents and skip file if they are identical. Use MD5 checksum for file comparison.
            Console.WriteLine("The file already exists.");
        }
        catch (IOException e)
        {
            Console.WriteLine($"An exception occurred:\nError code: " +
                              $"{e.HResult & 0x0000FFFF}\nMessage: {e.Message}");
        }

    }


    static void Main()
    {

        Console.Write("Set folder source path: ");
        string source = Console.ReadLine();
        Console.Write("Set folder destination path: ");
        string destination = Console.ReadLine();

        CopyDirectory(source, destination, true);

            
    }
}
//CopyDirectory(@".\", @".\copytest", true);

