using System;

namespace ConsoleApp2
{    
    public class CommandsHelper
    {
        public enum Command
        {
            Quit,
            IncompleteConnect,
            ConnectWithArguments,
            Help,
            About,
            History,
            NotImplemented,
            NotRecognised
        }

        public ConnectCommandParseResult LastConnectCommandParseResult = new ConnectCommandParseResult();

        public Command Parse(string userText)
        {
            if (userText == "quit")
                return Command.Quit;

            if (userText == "exit")
                return Command.Quit;

            if (userText == "history")
                return Command.History;

            if (userText == "help")
                return Command.Help;

            if (userText == "about")
                return Command.About;

            if (userText == "connect")
                return Command.IncompleteConnect;
            
            if (userText == "disconnect")
                return Command.NotImplemented;
            
            
            // ----- was this a valid 'connect' command ?? ------
            ConnectCommandParseResult temp = ValidConnectionCommand(userText);

            // if valid, then try to connect to server
            if (temp.Valid)
            {
                LastConnectCommandParseResult.IpAddress = temp.IpAddress;
                LastConnectCommandParseResult.Port = temp.Port;

                return Command.ConnectWithArguments;
            }
            
            // if get here, we don't recognise the command
            return Command.NotRecognised;
        }
        
        /// parse text from user
        /// IF
        /// 	in the form: connect <ip> <port>
        /// THEN
        /// 	return true
        ///
        /// else return false
        public ConnectCommandParseResult ValidConnectionCommand(string userText)
        {
            // defaults to Valid = false
            ConnectCommandParseResult result = new ConnectCommandParseResult();
            
            /*
             * -------------------------------------------------- BEGIN 
             * ------- quick hack to save matt typing connection details lots of times
             * REMOVE from final version !!!!!!!
             */
            if (userText == "localhost")
            {
                result.Valid = true;
                result.IpAddress = "127.0.0.1";
                result.Port = 11000;
                return result;
            }
            // ---------------------------------------------------- END 
            
            // 3 words
            // 1st = "connect"
            // 2nd = valid ip address
            // 3rd = port number
			
            string[] words = userText.Split(' ');

            // must be 3 words
            if(words.Length != 3)
                return result;

            // first word must be 'connect'
            if (words[0] != "connect")
                return result;

            // 3rd words must evaluate to an integer
            int userPort;
            if (Int32.TryParse(words[2], out userPort))
            {
                // seems okay - so set values ready to attempt to connect
                result.Valid = true;
                result.IpAddress = words[1];
                result.Port = userPort;
            }

            return result;
        }
        
        
        
        public void ShowHelp()
        {
            Console.WriteLine("CLIENT -- help --");
            Console.WriteLine("   Commands: ");
            Console.WriteLine("      quit / exit - terminate application");
            Console.WriteLine("      about - about this client app");
            Console.WriteLine("      history - see command history");
            Console.WriteLine("      Up/Down Arrow - cycle through command history");
            Console.WriteLine("      disconnect (not yet implemented - just quit/exit)");
            Console.WriteLine("      connect <ip> <port>");
            Console.WriteLine("      logs - see summary of all Unity Console log entries");
            Console.WriteLine("      log <id> - view details (including stack trace) for log entry <id>");
            Console.WriteLine("      emptylogs / clearlogs - empty the log of Unity Console messages");
            Console.WriteLine("      localhost - try to connect to 127.0.0.1 11000");
                        Console.WriteLine("");
            Console.WriteLine("   if no prompt is showing, just press ENTER");
            Console.WriteLine("   when connected, try: 'lua help' for LUA Simulator command list");
        }

        
        

    }
}