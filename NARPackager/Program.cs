using System;
using System.IO;
using System.Text;

namespace Nexon.Packager
{
    class Program
    {
        private static string basePath;
        private static string parentPath;
        private static Nexon.NexonArchiveFileEntryType storeType;
        private static Nexon.Extension.NexonArchiveExtension archive;


        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("Usage: Packager.exe <Folder Name> <Store Type: 0 - Raw, 1 - Encoded, 2 - Encoded and Compressed. Default: Encoded)");
                Console.In.ReadLine();
                return;
            }

            basePath = args[0];
            storeType = Nexon.NexonArchiveFileEntryType.Encoded;

            if (args.Length >= 2)
            {
                storeType = (Nexon.NexonArchiveFileEntryType)Int32.Parse(args[1]);
            }

            if (!Directory.Exists(basePath))
            {
                Console.WriteLine("{0} is not a valid directory.", basePath);
                Console.In.ReadLine();
                return;
            }
            parentPath = Directory.GetParent(basePath).FullName;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (archive = new Nexon.Extension.NexonArchiveExtension((new DirectoryInfo(basePath)).Name + ".nar"))
            {
                ProcessDirectory(basePath);

                // Save the archive.
                archive.Save();
            }

            Console.WriteLine("Done!");
            Console.In.ReadLine();
        }


        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        private static void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntry = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntry)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        private static void ProcessFile(string path)
        {
            if (archive == null)
                throw new ArgumentNullException(nameof(archive));

            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Processing file '{0}'.", path);

            var fileInfo = new FileInfo(path);
            using (FileStream inStream = fileInfo.OpenRead())
            {
                var entry = archive.Add(inStream, path.Replace(parentPath, "").Replace('\\', '/'), storeType, fileInfo.LastWriteTime);
                Console.WriteLine("Path: {0}", entry.Path);
                Console.WriteLine("Type: {0}", entry.StoreType);
            }

            Console.WriteLine("--------------------------------------------");
        }
    }
}
