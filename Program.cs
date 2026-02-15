using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClassicPersonaToolkit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args != null && args.Length > 0)
                {
                    RunCli(args);
                    return;
                }

                ShowMenu();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unhandled exception occurred: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }

        static void RunCli(string[] args)
        {
            var tokens = args.ToList();
            string command = tokens[0].Trim();
            var flags = ParseFlags(tokens.Skip(1).ToArray());

            if (command.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintCliHelp();
                return;
            }

            if (int.TryParse(command, out int legacyOption))
            {
                RunLegacyOptionFromCli(legacyOption.ToString(), flags);
                return;
            }

            switch (command.ToLowerInvariant())
            {
                case "extract-p2-bin":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output"); // directory
                        bool recursive = flags.ContainsKey("--recursive");

                        ExtractBinLike(input, recursive, f =>
                            Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(f, output));
                        break;
                    }

                case "extract-p2-gzbin":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output"); // directory
                        bool recursive = flags.ContainsKey("--recursive");

                        ExtractBinLike(input, recursive, f =>
                            Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(f, output));
                        break;
                    }

                case "extract-p2is-script-to-po":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output"); // directory
                        if (!Path.Exists(input)) Fail($"Input path not found: {input}");

                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Script(input, output);
                        break;
                    }

                case "extract-p2is-po-from-p2-bin":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output"); // directory
                        bool recursive = flags.ContainsKey("--recursive");

                        ExtractBinLike(input, recursive, f =>
                            Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2ScriptFromBin(f, output));
                        break;
                    }

                case "import-p2is-script-from-po":
                    {
                        var po = GetRequired(flags, "--po");
                        if (!File.Exists(po)) Fail($"PO file not found: {po}");

                        if (flags.TryGetValue("--out", out var outPath) && !string.IsNullOrWhiteSpace(outPath))
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(po, outPath);
                        else
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(po);

                        break;
                    }

                case "import-p2is-po-to-bin":
                    {
                        // Single:
                        //   import-p2is-po-to-bin --po file.po --bin file.bin [--out patched.bin]
                        // Directory:
                        //   import-p2is-po-to-bin --po-dir X --bin-dir Y [--out-dir Z]
                        if (flags.ContainsKey("--po-dir"))
                        {
                            var poDir = GetRequired(flags, "--po-dir");
                            var binDir = GetRequired(flags, "--bin-dir");
                            var outDir = GetOptional(flags, "--out-dir");
                            if (string.IsNullOrWhiteSpace(outDir)) outDir = poDir;

                            ImportPoDirToBinDir(poDir, binDir, outDir);
                        }
                        else
                        {
                            var po = GetRequired(flags, "--po");
                            var bin = GetRequired(flags, "--bin");
                            if (!File.Exists(po)) Fail($"PO file not found: {po}");
                            if (!File.Exists(bin)) Fail($"BIN file not found: {bin}");

                            if (flags.TryGetValue("--out", out var outPath) && !string.IsNullOrWhiteSpace(outPath))
                                Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(po, bin, outPath);
                            else
                                Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(po, bin);
                        }

                        break;
                    }

                case "import-p2is-po-to-gzbin":
                    {
                        var po = GetRequired(flags, "--po");
                        var gzbin = GetRequired(flags, "--gzbin");
                        var index = GetRequired(flags, "--index");

                        if (!File.Exists(po)) Fail($"PO file not found: {po}");
                        if (!File.Exists(gzbin)) Fail($"GZBIN file not found: {gzbin}");
                        if (!int.TryParse(index, out int containerIndex))
                            Fail($"Invalid index value: {index}");

                        if (flags.TryGetValue("--out", out var outPath) && !string.IsNullOrWhiteSpace(outPath))
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToGzBin(po, gzbin, containerIndex, outPath);
                        else
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToGzBin(po, gzbin, containerIndex);

                        break;
                    }

                case "import-p2-bin-from-dir":
                    {
                        var dir = GetRequired(flags, "--dir");
                        if (!Directory.Exists(dir)) Fail($"Directory not found: {dir}");
                        Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Bin(dir);
                        break;
                    }

                case "import-p2-gzbin-from-dir":
                    {
                        var dir = GetRequired(flags, "--dir");
                        if (!Directory.Exists(dir)) Fail($"Directory not found: {dir}");
                        Helpers.P2IS.PSP.P2ISHelper.ImportPersona2GzBin(dir);
                        break;
                    }

                default:
                    Fail($"Unknown command: {command}\n\nUse --help to see available commands.");
                    break;
            }
        }

        static void RunLegacyOptionFromCli(string option, Dictionary<string, string> flags)
        {

            switch (option)
            {
                case "1":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(input, output);
                        break;
                    }

                case "2":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        bool recursive = flags.ContainsKey("--recursive");
                        ExtractBinLike(input, recursive, f => Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(f, output));
                        break;
                    }

                case "3":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(input, output);
                        break;
                    }

                case "4":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        bool recursive = flags.ContainsKey("--recursive");
                        ExtractBinLike(input, recursive, f => Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(f, output));
                        break;
                    }

                case "7":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Script(input, output);
                        break;
                    }

                case "9":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2ScriptFromBin(input, output);
                        break;
                    }

                case "10":
                    {
                        var input = GetRequired(flags, "--input");
                        var output = GetOptional(flags, "--output");
                        bool recursive = flags.ContainsKey("--recursive");
                        ExtractBinLike(input, recursive, f => Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2ScriptFromBin(f, output));
                        break;
                    }

                case "11":
                    {
                        var po = GetRequired(flags, "--po");
                        if (!File.Exists(po)) Fail($"PO file not found: {po}");

                        if (flags.TryGetValue("--out", out var outPath) && !string.IsNullOrWhiteSpace(outPath))
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(po, outPath);
                        else
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(po);
                        break;
                    }

                case "12":
                    {
                        var poDir = GetRequired(flags, "--po-dir");
                        var outDir = GetOptional(flags, "--out-dir");
                        if (string.IsNullOrWhiteSpace(outDir)) outDir = poDir;
                        ImportPoDirToNewBin(poDir, outDir);
                        break;
                    }

                case "13":
                    {
                        if (flags.ContainsKey("--po-dir"))
                        {
                            var poDir = GetRequired(flags, "--po-dir");
                            var binDir = GetRequired(flags, "--bin-dir");
                            var outDir = GetOptional(flags, "--out-dir");
                            if (string.IsNullOrWhiteSpace(outDir)) outDir = poDir;
                            ImportPoDirToBinDir(poDir, binDir, outDir);
                        }
                        else
                        {
                            var po = GetRequired(flags, "--po");
                            var bin = GetRequired(flags, "--bin");
                            if (!File.Exists(po)) Fail($"PO file not found: {po}");
                            if (!File.Exists(bin)) Fail($"BIN file not found: {bin}");

                            if (flags.TryGetValue("--out", out var outPath) && !string.IsNullOrWhiteSpace(outPath))
                                Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(po, bin, outPath);
                            else
                                Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(po, bin);
                        }
                        break;
                    }

                case "14":
                    {
                        var poDir = GetRequired(flags, "--po-dir");
                        var binDir = GetRequired(flags, "--bin-dir");
                        var outDir = GetOptional(flags, "--out-dir");
                        if (string.IsNullOrWhiteSpace(outDir)) outDir = poDir;
                        ImportPoDirToBinDir(poDir, binDir, outDir);
                        break;
                    }

                case "15":
                    Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Bin(GetRequired(flags, "--dir"));
                    break;

                case "16":
                    ImportEachChildDirectory(GetRequired(flags, "--dir"),
                        child => Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Bin(child));
                    break;

                case "17":
                    Helpers.P2IS.PSP.P2ISHelper.ImportPersona2GzBin(GetRequired(flags, "--dir"));
                    break;

                case "18":
                    ImportEachChildDirectory(GetRequired(flags, "--dir"),
                        child => Helpers.P2IS.PSP.P2ISHelper.ImportPersona2GzBin(child));
                    break;

                case "19":
                    {
                        var po = GetRequired(flags, "--po");
                        var gzbin = GetRequired(flags, "--gzbin");
                        var index = GetRequired(flags, "--index");

                        if (!File.Exists(po)) Fail($"PO file not found: {po}");
                        if (!File.Exists(gzbin)) Fail($"GZBIN file not found: {gzbin}");
                        if (!int.TryParse(index, out int containerIndex))
                            Fail($"Invalid index value: {index}");

                        if (flags.TryGetValue("--out", out var outPath) && !string.IsNullOrWhiteSpace(outPath))
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToGzBin(po, gzbin, containerIndex, outPath);
                        else
                            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToGzBin(po, gzbin, containerIndex);
                        break;
                    }

                default:
                    Fail($"Legacy option not supported in CLI: {option}");
                    break;
            }
        }

        static void PrintCliHelp()
        {
            Console.WriteLine("ClassicPersonaToolkit CLI");
            Console.WriteLine();
            Console.WriteLine("Extraction (use --output for directory):");
            Console.WriteLine("  extract-p2-bin --input <file|dir> [--recursive] [--output <dir>]");
            Console.WriteLine("  extract-p2-gzbin --input <file|dir> [--recursive] [--output <dir>]");
            Console.WriteLine("  extract-p2is-script-to-po --input <file> [--output <dir>]");
            Console.WriteLine("  extract-p2is-po-from-p2-bin --input <file|dir> [--recursive] [--output <dir>]");
            Console.WriteLine();
            Console.WriteLine("Imports (files, use --out):");
            Console.WriteLine("  import-p2is-script-from-po --po <file.po> [--out <file.bin>]");
            Console.WriteLine("  import-p2is-po-to-bin --po <file.po> --bin <file.bin> [--out <patched.bin>]");
            Console.WriteLine("  import-p2is-po-to-bin --po-dir <dir> --bin-dir <dir> [--out-dir <dir>]");
            Console.WriteLine("  import-p2is-po-to-gzbin --po <file.po> --gzbin <file.bin> --index <N> [--out <patched.bin>]");
            Console.WriteLine("  import-p2-bin-from-dir --dir <dir>");
            Console.WriteLine("  import-p2-gzbin-from-dir --dir <dir>");
            Console.WriteLine();
        }

        static Dictionary<string, string> ParseFlags(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].Trim();
                if (!a.StartsWith("--", StringComparison.Ordinal)) continue;

                if (i + 1 < args.Length && !args[i + 1].Trim().StartsWith("--", StringComparison.Ordinal))
                {
                    dict[a] = TrimQuotes(args[i + 1].Trim());
                    i++;
                }
                else
                {
                    dict[a] = "true";
                }
            }

            return dict;
        }

        static string GetRequired(Dictionary<string, string> flags, string key)
        {
            if (!flags.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                Fail($"Missing required argument: {key}");
            return value;
        }

        static string GetOptional(Dictionary<string, string> flags, string key)
        {
            if (flags.TryGetValue(key, out var value))
                return value;
            return null;
        }

        static string TrimQuotes(string s) => s.Trim().Trim('"');

        static void Fail(string message)
        {
            Console.WriteLine(message);
            Environment.ExitCode = 2;
            throw new InvalidOperationException(message);
        }

        static void ExtractBinLike(string input, bool recursive, Action<string> fileAction)
        {
            if (File.Exists(input))
            {
                fileAction(input);
                return;
            }

            if (!Directory.Exists(input))
                Fail($"Input path not found: {input}");

            var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(input, "*.bin", opt);

            Console.WriteLine($"Found {files.Length} .bin files to process.");
            foreach (var f in files)
            {
                try
                {
                    Console.WriteLine($"Processing: {Path.GetFileName(f)}");
                    fileAction(f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(f)}: {ex.Message}");
                }
            }
        }

        static void ImportPoDirToNewBin(string poDir, string outDir)
        {
            if (!Directory.Exists(poDir)) Fail($"PO directory not found: {poDir}");
            Directory.CreateDirectory(outDir);

            var poFiles = Directory.GetFiles(poDir, "*.po", SearchOption.AllDirectories);
            Console.WriteLine($"Found {poFiles.Length} .po files to process.");

            foreach (var poFile in poFiles)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(poFile);
                    string outPath = Path.Combine(outDir, name + "_new.bin");
                    Console.WriteLine($"Processing: {Path.GetFileName(poFile)} -> {Path.GetFileName(outPath)}");
                    Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Script(poFile, outPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(poFile)}: {ex.Message}");
                }
            }
        }

        static void ImportPoDirToBinDir(string poDir, string binDir, string outDir)
        {
            if (!Directory.Exists(poDir)) Fail($"PO directory not found: {poDir}");
            if (!Directory.Exists(binDir)) Fail($"BIN directory not found: {binDir}");
            Directory.CreateDirectory(outDir);

            var poFiles = Directory.GetFiles(poDir, "*.po", SearchOption.AllDirectories);
            Console.WriteLine($"Found {poFiles.Length} .po files to process.");

            foreach (var poFile in poFiles)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(poFile);
                    string binPath = Path.Combine(binDir, name + ".bin");

                    if (!File.Exists(binPath))
                    {
                        Console.WriteLine($"Warning: BIN not found for {name}, skipping.");
                        continue;
                    }

                    string outPath = Path.Combine(outDir, name + "_patched.bin");
                    Console.WriteLine($"Processing: {Path.GetFileName(poFile)} + {Path.GetFileName(binPath)} -> {Path.GetFileName(outPath)}");
                    Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(poFile, binPath, outPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(poFile)}: {ex.Message}");
                }
            }
        }

        static void ImportEachChildDirectory(string parentDir, Action<string> action)
        {
            if (!Directory.Exists(parentDir)) Fail($"Directory not found: {parentDir}");

            var dirs = Directory.GetDirectories(parentDir, "*", SearchOption.TopDirectoryOnly);
            Console.WriteLine($"Found {dirs.Length} directories to process.");

            foreach (var d in dirs)
            {
                try
                {
                    Console.WriteLine($"Processing: {Path.GetFileName(d)}");
                    action(d);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(d)}: {ex.Message}");
                }
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
            Console.WriteLine("13. Import P2IS PO to P2 BIN [PSP]");
            Console.WriteLine("14. Import all P2IS PO to P2 BIN in directory [PSP]");
            Console.WriteLine("15. Import P2 BIN from directory [PSP]");
            Console.WriteLine("16. Import all P2 BIN from directories [PSP]");
            Console.WriteLine("17. Import P2 GZBIN from directory [PSP]");
            Console.WriteLine("18. Import all P2 GZBIN from directories [PSP]");
            Console.WriteLine("19. Import P2IS PO to P2 GZBIN [PSP]");
            Console.WriteLine("0. Exit");
            Console.Write("> ");
        }

        static void ProcessOption(string option)
        {
            try
            {
                string path = option switch
                {
                    "2" or "4" or "6" or "8" or "10" or "12" or "14" or "16" or "18" => GetPath("directory"),
                    "1" or "3" or "5" or "7" or "9" or "11" or "13" or "15" or "17" or "19" => GetPath("file or directory"),
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
                    case "13":
                        ImportPersona2ScriptToBinSingle(path);
                        break;
                    case "14":
                        ExtractDirectory(path, "13");
                        break;
                    case "15":
                        ImportPersona2BinSingle(path);
                        break;
                    case "16":
                        ExtractDirectory(path, "15");
                        break;
                    case "17":
                        ImportPersona2GzBinSingle(path);
                        break;
                    case "18":
                        ExtractDirectory(path, "17");
                        break;
                    case "19":
                        ImportPersona2ScriptToGzBinSingle(path);
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

        static void ImportPersona2ScriptToBinSingle(string poFilePath)
        {
            Console.Write("Write the original BIN file path: ");
            string binFilePath = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrEmpty(binFilePath) || !File.Exists(binFilePath))
            {
                Console.WriteLine("Invalid BIN file path.");
                return;
            }

            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(poFilePath, binFilePath);
        }

        static void ImportPersona2ScriptToGzBinSingle(string poFilePath)
        {
            Console.Write("Write the GZBIN file path: ");
            string gzBinFilePath = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrEmpty(gzBinFilePath) || !File.Exists(gzBinFilePath))
            {
                Console.WriteLine("Invalid GZBIN file path.");
                return;
            }

            Console.Write("Write the container index (e.g., 0 for e0000.bin): ");
            string indexStr = Console.ReadLine()?.Trim();

            if (!int.TryParse(indexStr, out int containerIndex))
            {
                Console.WriteLine("Invalid index.");
                return;
            }

            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToGzBin(poFilePath, gzBinFilePath, containerIndex);
        }

        static void ImportPersona2BinSingle(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Invalid directory path.");
                return;
            }

            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Bin(directoryPath);
        }

        static void ImportPersona2GzBinSingle(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Invalid directory path.");
                return;
            }

            Helpers.P2IS.PSP.P2ISHelper.ImportPersona2GzBin(directoryPath);
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

            if (fileOption == "13")
            {
                var poFiles = Directory.GetFiles(dirPath, "*.po", SearchOption.AllDirectories);
                Console.WriteLine($"Found {poFiles.Length} .po files to process.\n");

                Console.Write("Write the original BIN files directory: ");
                string binDirPath = Console.ReadLine()?.Trim('"');

                if (string.IsNullOrEmpty(binDirPath) || !Directory.Exists(binDirPath))
                {
                    Console.WriteLine("Invalid BIN directory path.");
                    return;
                }

                foreach (var poFile in poFiles)
                {
                    try
                    {
                        Console.WriteLine($"Processing: {Path.GetFileName(poFile)}");

                        string poFileName = Path.GetFileNameWithoutExtension(poFile);
                        string binFilePath = Path.Combine(binDirPath, poFileName + ".bin");

                        if (!File.Exists(binFilePath))
                        {
                            Console.WriteLine($"Warning: Corresponding BIN file not found for {poFileName}. Skipping.\n");
                            continue;
                        }

                        string outputDir = Path.GetDirectoryName(poFile);
                        string outputPath = Path.Combine(outputDir, poFileName + "_patched.bin");

                        Helpers.P2IS.PSP.P2ISHelper.ImportPersona2ScriptToBin(poFile, binFilePath, outputPath);

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

            if (fileOption == "15")
            {
                var directories = Directory.GetDirectories(dirPath, "*", SearchOption.TopDirectoryOnly);
                Console.WriteLine($"Found {directories.Length} directories to process.\n");

                foreach (var dir in directories)
                {
                    try
                    {
                        Console.WriteLine($"Processing: {Path.GetFileName(dir)}");
                        Helpers.P2IS.PSP.P2ISHelper.ImportPersona2Bin(dir);
                        Console.WriteLine($"Completed: {Path.GetFileName(dir)}\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {Path.GetFileName(dir)}: {ex.Message}\n");
                    }
                }

                Console.WriteLine("All directories processed!");
                return;
            }

            if (fileOption == "17")
            {
                var directories = Directory.GetDirectories(dirPath, "*", SearchOption.TopDirectoryOnly);
                Console.WriteLine($"Found {directories.Length} directories to process.\n");

                foreach (var dir in directories)
                {
                    try
                    {
                        Console.WriteLine($"Processing: {Path.GetFileName(dir)}");
                        Helpers.P2IS.PSP.P2ISHelper.ImportPersona2GzBin(dir);
                        Console.WriteLine($"Completed: {Path.GetFileName(dir)}\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {Path.GetFileName(dir)}: {ex.Message}\n");
                    }
                }

                Console.WriteLine("All directories processed!");
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