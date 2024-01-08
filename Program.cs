namespace ClassicPersonaToolkit
{
    using System;

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
            try
            {
                Console.Clear();
                PrintHeader();

                Console.WriteLine("Select an option:");
                Console.WriteLine("1. Test Function 1 (Extract P2 BIN)");
                Console.WriteLine("2. Test Function 2 (Extract all P2 BIN in a dir)");
                Console.WriteLine("3. Test Function 3 (Extract P2 GZBIN)");
                Console.WriteLine("4. Test Function 4 (Extract all P2 GZBIN in a dir)");
                Console.WriteLine("0. Exit");
                Console.Write("> ");

                char option = Console.ReadKey().KeyChar;
                Console.WriteLine();

                switch (option)
                {
                    case '1':
                        Console.Clear();
                        try
                        {
                            Console.Write("Write the file path: ");
                            string filePath = Console.ReadLine();
                            if (filePath != "")
                                if (Path.Exists(filePath))
                                    Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(filePath);
                                else
                                    Console.WriteLine("No file exists in this dir.");
                            else
                                Console.WriteLine("No file path detected.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An exception occurred: {ex.Message}");
                        }
                        break;
                    case '2':
                        Console.Clear();
                        try
                        {
                            Console.Write("Write the dir path: ");
                            string dirPath = Console.ReadLine();
                            if (dirPath != "")
                                if (Path.Exists(dirPath))
                                    foreach (string filePath in Directory.GetFiles(dirPath, "*.bin"))
                                    {
                                        try
                                        {
                                            if (filePath.EndsWith(".bin", StringComparison.Ordinal))
                                                Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2Bin(filePath);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"An exception occurred: {ex.Message}");
                                        }                                        
                                    }
                                else
                                    Console.WriteLine("No file exists in this dir.");
                            else
                                Console.WriteLine("No file path detected.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An exception occurred: {ex.Message}");
                        }
                        break;
                    case '3':
                        Console.Clear();
                        try
                        {
                            Console.Write("Write the file path: ");
                            string filePath = Console.ReadLine();
                            if (filePath != "")
                                if (Path.Exists(filePath))
                                    Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(filePath);
                                else
                                    Console.WriteLine("No file exists in this dir.");
                            else
                                Console.WriteLine("No file path detected.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An exception occurred: {ex.Message}");
                        }
                        break;
                    case '4':
                        Console.Clear();
                        try
                        {
                            Console.Write("Write the dir path: ");
                            string dirPath = Console.ReadLine();
                            if (dirPath != "")
                                if (Path.Exists(dirPath))
                                    foreach (string filePath in Directory.GetFiles(dirPath, "*.bin"))
                                    {
                                        try
                                        {
                                            if (filePath.EndsWith(".bin", StringComparison.Ordinal))
                                                Helpers.P2IS.PSP.P2ISHelper.ExtractPersona2GzBin(filePath);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"An exception occurred: {ex.Message}");
                                        }
                                    }
                                else
                                    Console.WriteLine("No file exists in this dir.");
                            else
                                Console.WriteLine("No file path detected.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An exception occurred: {ex.Message}");
                        }
                        break;
                    case '0':
                        Console.WriteLine("Exiting the program. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please select a valid option.");
                        break;
                }

                if (option != '0')
                {
                    Console.Write("(Press Enter to continue)");
                    Console.ReadLine();
                    ShowMenu();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred in ShowMenu: {ex.Message}");
            }
        }

        static void PrintHeader()
        {
            Console.WriteLine("***********************************************");
            Console.WriteLine("              ClassicPersonaToolkit            ");
            Console.WriteLine("                                               ");
            Console.WriteLine($"                  Year: {DateTime.Now.Year}           ");
            Console.WriteLine("           Thanks to Pleonex for Yarhl         ");
            Console.WriteLine("***********************************************");
            Console.WriteLine();
        }
    }

}
