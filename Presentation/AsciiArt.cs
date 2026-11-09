namespace TatumWelcome2.Presentation;

public static class AsciiArt
{
    public static void ShowWelcome()
    {
        Console.WriteLine(@"
==========================================
 _____      _      _____   _   _   __  __ 
|_   _|    / \    |_   _| | | | | |  \/  |
  | |     / _ \     | |   | | | | | |\/| |
  | |    / ___ \    | |   | |_| | | |  | |
  |_|   /_/   \_\   |_|    \___/  |_|  |_|

                B  A  N  K
==========================================
      Welcome. Please log in to begin.
");
    }

    public static void ShowGoodbye()
    {
        Console.WriteLine(@"
==========================================
        Thank you for banking with
                TATUM BANK
            We'll see you next time!
==========================================
");
    }
}