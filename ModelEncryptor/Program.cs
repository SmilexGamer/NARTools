using System;
using System.IO;
using System.Text;

namespace Nexon.CSO.ModelEncryptor
{
    class Program
    {
        private static string fileName;
        private static int version;
        private static bool backup;

        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("Usage: ModelEncryptor.exe <FileName.mdl> <Encryption Version - 20 or 21. Default: 20> <Backup decrypted files - 1 or 0. Default: 1>");
                Console.In.ReadLine();
                return;
            }

            fileName = args[0];
            version = 20;
            backup = true;

            if (args.Length >= 2)
            {
                version = Int32.Parse(args[1]);

                if (version != 20 && version != 21)
                {
                    Console.WriteLine("{0} is not a valid encryption version. Valid Encryption versions: 20, 21.", version);
                    Console.In.ReadLine();
                    return;
                }
            }

            if (args.Length >= 3)
            {
                backup = Convert.ToBoolean(Int32.Parse(args[2]));
            }

            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Processing file '{0}'.", fileName);
            Console.WriteLine("Encryption Version: {0}", version);
            Console.WriteLine("--------------------------------------------");

            FileInfo file = new FileInfo(fileName);
            if (file.Exists)
            {
                // Create a temp file (a copy of the real file).
                FileInfo tempFile = null;
                do
                {
                    tempFile = new FileInfo(Path.Combine(Path.GetDirectoryName(file.DirectoryName), Path.ChangeExtension(Path.GetTempFileName(), ".mdl")));
                } while (tempFile.Exists);
                file.CopyTo(tempFile.FullName);

                try
                {
                    ModelResult result = ModelHelper.EncryptModel(tempFile.FullName, version);
                    if (result == ModelResult.Success)
                    {
                        // Backup the real file.
                        if (backup)
                        {
                            FileInfo backupFile = new FileInfo(fileName + ".bak");
                            file.CopyTo(backupFile.FullName);
                        }

                        // Replace the real file's data with the temp file's data.
                        tempFile.CopyTo(file.FullName, true);
                        Console.WriteLine("Done!");
                    }
                    else
                    {
                        Console.WriteLine("An error occured while encrypting the model: '{0}'", result);
                    }
                }
                finally
                {
                    // Delete the temp file.
                    tempFile.Refresh();
                    if (tempFile.Exists)
                        tempFile.Delete();
                }
            }
            else
            {
                Console.WriteLine("The file '{0}' doesn't exist.", fileName);
            }
            Console.In.ReadLine();
        }
    }
}
