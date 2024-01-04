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
                Console.WriteLine("1. Test Function 1");
                Console.WriteLine("2. Test Function 2");
                Console.WriteLine("3. Exit");
                Console.Write("> ");

                char option = Console.ReadKey().KeyChar;
                Console.WriteLine();

                switch (option)
                {
                    case '1':
                        Function1();
                        break;
                    case '2':
                        Function2();
                        break;
                    case '3':
                        Console.WriteLine("Exiting the program. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please select a valid option.");
                        break;
                }

                if (option != '3')
                {
                    ShowMenu();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred in ShowMenu: {ex.Message}");
            }
        }

        static void Function1()
        {
            Console.Clear();
            try
            {
                // This will be eliminated, this will call a converter to a test format.
                Console.WriteLine("You selected Function 1.");
                Console.WriteLine("Press any key to return to the menu...");
                Console.ReadKey();
                //
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred in Function1: {ex.Message}");
            }
        }

        static void Function2()
        {
            Console.Clear();
            try
            {
                // This will be eliminated, this will call a converter to a test format.
                Console.WriteLine("You selected Function 2.");
                Console.WriteLine("Press any key to return to the menu...");
                Console.ReadKey();
                //
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred in Function2: {ex.Message}");
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
