using System;

namespace ConsoleApp2
{
    /*
     * keep reading characters from Console until ENTER
     * works with:
     * - backspace to delete left of cursor
     * - Up/Down arrows for scrolling through a Command History
     *
     * expected 'prompt' to be provided
     */
    class HistoryConsole
    {

        public static string ReadConsole(string prompt)
        {
            string currentText = "";
            Console.Write(prompt);
 
            while (true)
            {
                // loop until Enter key is pressed
                ConsoleKeyInfo KeyInfoPressed = Console.ReadKey();
                switch (KeyInfoPressed.Key)
                {
                    case ConsoleKey.UpArrow:
                        currentText = CommandHistory.GetAtCursor();
                        CommandHistory.Back();
                        ClearConsole(prompt, currentText);
                        break;
 
                    case ConsoleKey.DownArrow:
                        CommandHistory.Forward();
                        currentText = CommandHistory.GetAtCursor();
                        ClearConsole(prompt, currentText);
                        break;
 
                    case ConsoleKey.Backspace:
                        Boolean someTextToDelete = (currentText.Length > 0) && (Console.CursorLeft > prompt.Length - 1);

                        if (someTextToDelete)
                        {
                            currentText = currentText.Remove(currentText.Length - 1, 1);
                            ClearConsole(prompt, currentText);
                        }
                        else
                        {
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }
                        break;

                    case ConsoleKey.Delete:
                        break;
 
                    default:
                        // just add the char to the answer building string
                        currentText = currentText + KeyInfoPressed.KeyChar.ToString();
                        ClearConsole(prompt, currentText);
                        break;
 
                    case ConsoleKey.Enter:
                        // exit this routine and return the Answer to process further
                        // set comamnd history cursor back to top of stack
                        CommandHistory.CursorToTop();
                        // output current text
                        Console.Write("\n");
//                        Console.Write(prompt);
                        
                        // if not empty string, then add this to Command history
                        if (currentText.Length > 0)
                        {
                            CommandHistory.Add(currentText);
                        }
                        return currentText;
                }
            }
        }
 
 
        /// <summary>
        /// In this function we will clear the currentline:
        ///  1. we will set the cursur position to the 1 position
        ///  2. clear the line question length will be substracted from line width
        ///  3. reset the cursur position to 1 again
        ///  4. input the question and answer of the user
        /// </summary>
        /// <param name="prompt">Question which is served at the user</param>
        /// <param name="command">Answer of the user</param>
        public static void ClearConsole(string prompt, string command)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new String(' ', Console.BufferWidth - prompt.Length));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(prompt + command);
        }
    }
}