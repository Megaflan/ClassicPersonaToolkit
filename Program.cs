namespace ClassicPersonaToolkit
{
    using System;
    using System.IO;
    using System.Linq;

    internal class Program
    {
        static void Main()
        {
            try
            {
                ShowMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unhandled exception occurred: {ex.Message}");
            }
        }

        static void ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                PrintHeader();
                PrintMenuOptions();

                string option = Console.ReadLine()?.Trim();

                if (option == "0") break;

                ProcessOption(option);

                Console.WriteLine("(Press Enter to continue)");
                Console.ReadLine();
            }

            Console.WriteLine("Exiting the program. Goodbye!");
        }

        static void PrintHeader()
        {
            Console.WriteLine("***********************************************");
            Console.WriteLine("              ClassicPersonaToolkit            ");
            Console.WriteLine($"                  Year: {DateTime.Now.Year}           ");
            Console.WriteLine("           Thanks to Pleonex for Yarhl         ");
            Console.WriteLine("***********************************************\n");
        }

        static void PrintMenuOptions()
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("1. Extract P2 BIN [PSP]");
            Console.WriteLine("2. Extract all P2 BIN in directory [PSP]");
            Console.WriteLine("3. Extract P2 GZBIN [PSP]");
            Console.WriteLine("4. Extract all P2 GZBIN in directory [PSP]");
            Console.WriteLine("5. Extract P1 BIN [PSP]");
            Console.WriteLine("6. Extract all P1 BIN in directory [PSP]");
            Console.WriteLine("7. Extract P2IS Script to PO [PSP]");
            Console.WriteLine("8. Extract P1 Game Data [PSP]");
            Console.WriteLine("9. Extract P2IS PO from P2 BIN [PSP]");
            Console.WriteLine("10. Extract all P2IS PO from P2 BIN in directory [PSP]");
            Console.WriteLine("11. Import P2IS Script from PO [PSP]");
            Console.WriteLine("12. Import all P2IS Scripts from PO in directory [PSP]");
            Console.WriteLine("0. Exit");
            Console.Write("> ");
        }

        static void ProcessOption(string option)
        {
            try
            {
                string path = option switch
                {
                    "2" or "4" or "6" or "8" or "10" or "12" => GetPath("directory"),
                    "1" or "3" or "5" or "7" or "9" or "11" => GetPath("file"),
                    "0" => null,
                    _ => null
                };

                if (option == "0") return;

                if (string.IsNullOrEmpty(path) || !Path.Exists(path))
                {
                    Console.WriteLine("Invalid path.");
                    return;
                }

                switch (option)
                {
                    case "1":
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(path);
                        break;
                    case "2":
                        ExtractDirectory(path, "1");
                        break;
                    case "3":
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(path);
                        break;
                    case "4":
                        ExtractDirectory(path, "3");
                        break;
                    case "5":
                        Helpers.P1.PSP.P1Helper.ExtractPersona1Bin(path);
                        break;
                    case "6":
                        ExtractDirectory(path, "5");
                        break;
                    case "7":
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Script(path);
                        break;
                    case "8":
                        Helpers.P1.PSP.P1Helper.ExtractPersona1Game(path);
                        break;
                    case "9":
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2ScriptFromBin(path);
                        break;
                    case "10":
                        ExtractDirectory(path, "9");
                        break;
                    case "11":
                        ImportPersona2ScriptSingle(path);
                        break;
                    case "12":
                        ExtractDirectory(path, "11");
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred: {ex.Message}");
            }
        }

        static string GetPath(string type)
        {
            Console.Write($"Write the {type} path: ");
            return Console.ReadLine()?.Trim('"');
        }

        static void ImportPersona2ScriptSingle(string poFilePath)
        {
            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(poFilePath);
        }

        static void ExtractDirectory(string dirPath, string fileOption)
        {
            if (fileOption == "11")
            {
                var poFiles = Directory.GetFiles(dirPath, "*.po", SearchOption.AllDirectories);
                Console.WriteLine($"Found {poFiles.Length} .po files to process.\n");

                foreach (var poFile in poFiles)
                {
                    try
                    {
                        Console.WriteLine($"Processing: {Path.GetFileName(poFile)}");

                        string outputDir = Path.GetDirectoryName(poFile);
                        string poFileName = Path.GetFileNameWithoutExtension(poFile);
                        string outputPath = Path.Combine(outputDir, poFileName + "_new.bin");

                        Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(poFile, outputPath);

                        Console.WriteLine($"Completed: {Path.GetFileName(poFile)}\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {Path.GetFileName(poFile)}: {ex.Message}\n");
                    }
                }

                Console.WriteLine("All files processed!");
                return;
            }

            var files = Directory.GetFiles(dirPath, "*.bin", SearchOption.AllDirectories);

            Console.WriteLine($"Found {files.Length} .bin files to process.\n");

            foreach (var file in files)
            {
                try
                {
                    Console.WriteLine($"Processing: {Path.GetFileName(file)}");

                    switch (fileOption)
                    {
                        case "1":
                            Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(file);
                            break;
                        case "3":
                            Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(file);
                            break;
                        case "5":
                            Helpers.P1.PSP.P1Helper.ExtractPersona1Bin(file);
                            break;
                        case "9":
                            Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2ScriptFromBin(file);
                            break;
                    }

                    Console.WriteLine($"Completed: {Path.GetFileName(file)}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(file)}: {ex.Message}\n");
                }
            }

            Console.WriteLine("All files processed!");
        }
    }
}