using System;

namespace ConsoleApp2
{
    /*
     * class to manage a history of the most recent commands typed in the terminal
     */
    public class CommandHistory
    {
        // max number of commands (before looping)       
        private const int MAX = 100;
        
        private static string[] commands = new string[MAX];

        private static int top = 0;
        private static int cursor = 0;

        /*
         * add new command to the history
         */
        public static void Add(string command)
        {
            if (top < MAX)
            {
                commands[top] = command;
                top++;                
            }

            if (top >= MAX)
            {
                top = 0;
            }
        }

        /*
         * move cursor to top of history
         */
        public static void CursorToTop()
        {
            cursor = top;
        }

        /*
         * return the command at the current cursor position
         */
        public static string GetAtCursor()
        {
            return commands[cursor];
        }

        /*
         * move cursor back in history
         */
        public static void Back()
        {
            cursor--;
            if (cursor < 0)
                cursor = top;
        }

        /*
         * move cursor forward in history
         */
        public static void Forward()
        {
            cursor++;
            if (cursor >= top)
                cursor = 0;
        }

        /*
         * list all the commands in the history
         */
        public static void PrintAll()
        {
            Console.WriteLine("List commands in the client console history:");
            for (int i = top - 1; i >= 0; i--)
            {
                Console.WriteLine("    [" + i + "] = '" + commands[i] + "'");
            }
        }

        /*
         * display entry at current cursor position
         */
        public static void PrintCursor()
        {
            Console.WriteLine("[" + cursor + "] = '" + commands[cursor] + "', get at cursor = '" + GetAtCursor() + "'");
            
        }

    }
}
