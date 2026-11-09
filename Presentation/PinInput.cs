
namespace TatumWelcome2.Presentation;

public static class PinInput
{
    public static string ReadHiddenPin()
    {
        string pin = "";

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return pin;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (pin.Length > 0)
                {
                    pin = pin[..^1];
                    Console.Write("\b \b");
                }

                continue;
            }

            if (key.KeyChar >= '0' &&
                key.KeyChar <= '9' &&
                pin.Length < 4)
            {
                pin += key.KeyChar;
                Console.Write("*");
            }
        }
    }
}
