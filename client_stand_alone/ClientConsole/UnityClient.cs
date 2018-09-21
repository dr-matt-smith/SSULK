/*
 * TODO:
 *
 * - implement user Disconnect
 * - cope with lost connection
 * - add timeout if nothing happens when trying to connect - RunState_WaitingForConnection()
 */

using System;

using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConsoleApp2
{
	/*
	 * class to:
	 *
	 * - run a console (with a history)
	 * - connect as a m_client to a Unity server
	 */
	class UnityClient
	{
		private State m_state = State.START;
		public enum State
		{
			START,
			ENTER_USER_INPUT,
			USER_INPUT,
			PARSE_USER_COMMAND,
			PROCESS_USER_COMMAND,
			SENDING_TO_SERVER,
			PROCESS_RECEIVED_MESSAGE,
			WAIT_FOR_REPLY_FROM_SERVER,
			WAITING_FOR_CONNECTION,
			TERMINATE
		}
		
		private ClientEvent m_lastEvent = ClientEvent.Consumed;
		public enum ClientEvent
		{
			Consumed,
			QuitCommandEntered,
			UserCommandEntered,
			IgnorableUserCommandEntered,
			MessageReceivedFromServer,
			ConnectionAttempted,
			ConnectionLost,
			ConnectionFailed,
			ConnectionSuccess,
			DisplayedSimpleMessage,
			MessageReadyToSendToServer,
			MessageSentToServer,
			ReadyToTryConnection,
			ErrorMessageFailedToSend,
			SocketError
		}
		
		
		private string m_prompt = "(not connected)> ";

		private TcpClient m_client;
		private Thread m_clientReceiveThread;


		private bool m_applicationQuit = false;

		private CommandsHelper m_commandsHelper;

		private bool m_connected = false;


		private bool m_readingLineFromUser = false;

		private string m_userText = "";
		private string m_incompleteTyping = "";

		private string m_ipAddress = "";
		private int m_port;
		

		public UnityClient()
		{
			m_commandsHelper = new CommandsHelper();
			Console.WriteLine("m_client:: trying to connect to server ...");
		}

		public void Run()
		{
			while (m_state != State.TERMINATE)
			{
				MainConsoleLoop();

//				try
//				{
//					MainConsoleLoop();
//				}
//				catch
//				{
//					Console.WriteLine("(error occurred)");
//					m_state = State.ENTER_USER_INPUT;
//				}
			}
			
			
			// ---- shut down the application 
			Environment.Exit(-1);

		}

		private void MainConsoleLoop()
		{
// see state in prompt ------ remove <<<<<<
//m_prompt = m_state.ToString() + "> ";

			switch (m_state)
			{
				case State.START:
					m_state = State.ENTER_USER_INPUT;
					break;

				case State.ENTER_USER_INPUT:
					RunState_EnterUserInput();
					break;

				case State.USER_INPUT:
					RunState_UserInput();
					break;

				case State.PARSE_USER_COMMAND:
					RunState_ParseUserCommand();
					break;

				case State.PROCESS_USER_COMMAND:
					RunState_ProcessUserCommand();
					break;

				case State.WAITING_FOR_CONNECTION:
					RunState_WaitingForConnection();
					break;

				case State.SENDING_TO_SERVER:
					RunState_SendingToServer();
					break;

				case State.WAIT_FOR_REPLY_FROM_SERVER:
					RunState_WaitForReplyFromServer();
					break;
			}
	}


		/*
		 * starting user input - display prompt
		 */
		private void RunState_EnterUserInput()
		{
			// restarting user input - so we need to show the user the prompt again
			Console.Write(m_prompt);
			m_incompleteTyping = "";
			
			// go straight into next state
			m_state = State.USER_INPUT;
			
		}

		private void RunState_UserInput()
		{
			// (1) do something
			m_userText = ReadConsole();
			
			// (2) check events
			if (m_lastEvent == ClientEvent.UserCommandEntered)
			{
				m_lastEvent = ClientEvent.Consumed;
				m_state = State.PARSE_USER_COMMAND;
			}

			if (m_lastEvent == ClientEvent.IgnorableUserCommandEntered)
			{
				m_lastEvent = ClientEvent.Consumed;
				m_state = State.ENTER_USER_INPUT;				
			}
		}

		
		/*
		 * decide what to do with contents of m_userText
		 */
		private void RunState_ParseUserCommand()
		{
			// detect type of command
			CommandsHelper.Command commandType = m_commandsHelper.Parse(m_userText);

			// try to process
			switch (commandType)
			{
				case CommandsHelper.Command.Quit:
					m_lastEvent = ClientEvent.QuitCommandEntered;
					break;

				case CommandsHelper.Command.Help:
					m_commandsHelper.ShowHelp();
					m_lastEvent = ClientEvent.DisplayedSimpleMessage;
					break;

				case CommandsHelper.Command.About:
					Console.WriteLine("(version 0.1 - Sep 2018) Stand along console client - to connect to Unity Server ");				
					m_lastEvent = ClientEvent.DisplayedSimpleMessage;
					break;

				case CommandsHelper.Command.History:
					CommandHistory.PrintAll();
					m_lastEvent = ClientEvent.DisplayedSimpleMessage;
					break;

				case CommandsHelper.Command.IncompleteConnect:
					Console.WriteLine("usage: connect <ip> <port>");				
					m_lastEvent = ClientEvent.DisplayedSimpleMessage;
					break;

				case CommandsHelper.Command.ConnectWithArguments:
					m_ipAddress = m_commandsHelper.LastConnectCommandParseResult.IpAddress;
					m_port = m_commandsHelper.LastConnectCommandParseResult.Port;
					m_lastEvent = ClientEvent.ReadyToTryConnection;
					break;

				case CommandsHelper.Command.NotImplemented:
					Console.WriteLine("sorry - that command isn't implemented yet");				
					m_lastEvent = ClientEvent.DisplayedSimpleMessage;
					break;
				
				case CommandsHelper.Command.NotRecognised:
					// connected, and not a simple client command, so send text on to the Server
					if (m_connected)
					{
						m_lastEvent = ClientEvent.MessageReadyToSendToServer;
					}
					else
					{
						// not connected - so tell user to connect
						Console.Write("you need to connect to a game server...");
						Console.WriteLine("// connect <ip> <port>");

						m_lastEvent = ClientEvent.DisplayedSimpleMessage;						
					}
					break;
				
				default:
					Console.Write("(sorry - command not recognised)");
					m_lastEvent = ClientEvent.DisplayedSimpleMessage;
					break;										
			}
									
			// now EXECUTE command based on event message
			m_state = State.PROCESS_USER_COMMAND;

		}

		private void RunState_ProcessUserCommand()
		{
			switch (m_lastEvent)
			{
				case ClientEvent.QuitCommandEntered:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.TERMINATE;
					break;

				case ClientEvent.DisplayedSimpleMessage:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.ENTER_USER_INPUT;
					break;
				
				case ClientEvent.ReadyToTryConnection:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.WAITING_FOR_CONNECTION;

					// try to connect
					ConnectToTcpServer(m_ipAddress, m_port);
					break;
				
				case ClientEvent.MessageReadyToSendToServer:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.SENDING_TO_SERVER;
					SendMessageToServer(m_userText);							
					break;

				default:
					break;
			}
		}

		private void RunState_SendingToServer()
		{
			switch (m_lastEvent)
			{
				case ClientEvent.MessageSentToServer:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.WAIT_FOR_REPLY_FROM_SERVER;
					break;

				case ClientEvent.ErrorMessageFailedToSend:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.ENTER_USER_INPUT;
					break;
			}
		}

		private void RunState_WaitingForConnection()
		{
			switch (m_lastEvent)
			{
				case ClientEvent.ConnectionSuccess:
					m_lastEvent = ClientEvent.Consumed;
					m_prompt = "(connected)> ";
					m_state = State.ENTER_USER_INPUT;
					break;

				case ClientEvent.ConnectionFailed:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.ENTER_USER_INPUT;
					break;
			}
			
			// time out test  ???????
		}
		
		
		private void RunState_WaitForReplyFromServer()
		{
			switch (m_lastEvent)
			{
				case ClientEvent.MessageReceivedFromServer:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.ENTER_USER_INPUT;	
					break;

				case ClientEvent.SocketError:
					m_lastEvent = ClientEvent.Consumed;
					m_state = State.ENTER_USER_INPUT;
					break;
			}

		}



		/*
		 * CONNECT to server
		 */
		private void ConnectToTcpServer(string ipAddress, int port)
		{
			Console.WriteLine("trying to connect to: " + ipAddress + ":" + port);

			try
			{
				m_client = new TcpClient(ipAddress, port);

				m_clientReceiveThread = new Thread(ReceiveFromServer);
				m_clientReceiveThread.IsBackground = true;
				m_clientReceiveThread.Start();

				Console.WriteLine("success, m_connected to: " + ipAddress + ":" + port);
				m_prompt = "(connected)> ";
				m_connected = true;

				m_lastEvent = ClientEvent.ConnectionSuccess;
			}
			catch (Exception e)
			{
				m_connected = false;
				Console.WriteLine("Failure - could not connect to: " + ipAddress + ":" + port);
				m_prompt = "(not connected)> ";

				m_lastEvent = ClientEvent.ConnectionFailed;
			}
			

		}
	
		/*
		* receive message FROM server
		*/
		private void ReceiveFromServer()
		{
			try
			{
				Byte[] bytes = new Byte[1024];
				while (true)
				{
					// Get a stream object for reading 				
					using (NetworkStream stream = m_client.GetStream())
					{
						int length;
						// Read incoming stream into byte array. 					
						while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
						{
							var incommingData = new byte[length];
							Array.Copy(bytes, 0, incommingData, 0, length);
							// Convert byte array to string message. 						
							string serverMessage = Encoding.ASCII.GetString(incommingData);
							Console.WriteLine("");
							Console.WriteLine("-------- received from server -----------");
							Console.WriteLine(serverMessage);
//							Console.WriteLine("(press ENTER to continue)");

							m_lastEvent = ClientEvent.MessageReceivedFromServer;
						}
					}
				}
			}
			catch (SocketException socketException)
			{
				Console.WriteLine("ERROR - Socket exception: " + socketException);
				m_lastEvent = ClientEvent.SocketError;
			}
		}
		
		
		/*
		 * send message TO server
		 */
		private void SendMessageToServer(string clientMessage)
		{
			if (m_client == null)
			{
				Console.WriteLine("m_client: connection was null, could not send message");
				m_connected = false;
				m_state = State.ENTER_USER_INPUT;
				return;
			}

			try
			{
				// Get a stream object for writing. 			
				NetworkStream stream = m_client.GetStream();
				if (stream.CanWrite)
				{
					// Convert string message to byte array.                 
					byte[] m_clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
					// Write byte array to socketConnection stream.                 
					stream.Write(m_clientMessageAsByteArray, 0, m_clientMessageAsByteArray.Length);

					m_lastEvent = ClientEvent.MessageSentToServer;
				}
			}
			catch (SocketException socketException)
			{
				Console.WriteLine("m_client: Socket exception: " + socketException);
				m_lastEvent = ClientEvent.ErrorMessageFailedToSend;
			}
		}
		
		
		/*
		 * ------- console reading stuff --------
		 */
		public string ReadConsole()
		{
			/*
			 * if had an event that displayed text on the console, we need to re-display prompt and any typing
			 */
			if (m_lastEvent == ClientEvent.MessageReceivedFromServer)
			{
				m_lastEvent = ClientEvent.Consumed;
				ClearConsole(m_incompleteTyping);
			}

			// loop until Enter key is pressed
			ConsoleKeyInfo KeyInfoPressed = Console.ReadKey();
			switch (KeyInfoPressed.Key)
			{
				case ConsoleKey.UpArrow:
					m_incompleteTyping = CommandHistory.GetAtCursor();
					CommandHistory.Back();
					ClearConsole(m_incompleteTyping);
					break;

				case ConsoleKey.DownArrow:
					CommandHistory.Forward();
					m_incompleteTyping = CommandHistory.GetAtCursor();
					ClearConsole(m_incompleteTyping);
					break;

				case ConsoleKey.Backspace:
					Boolean someTextToDelete = (m_incompleteTyping.Length > 0) && (Console.CursorLeft > m_prompt.Length - 1);

					if (someTextToDelete)
					{
						m_incompleteTyping = m_incompleteTyping.Remove(m_incompleteTyping.Length - 1, 1);
						ClearConsole(m_incompleteTyping);
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
					m_incompleteTyping = m_incompleteTyping + KeyInfoPressed.KeyChar.ToString();
					ClearConsole(m_incompleteTyping);
					break;

				case ConsoleKey.Enter:
					// exit this routine and return the Answer to process further
					// set command history cursor back to top of stack
					CommandHistory.CursorToTop();
					// output current text
					Console.Write("\n");

					// if not empty string, then add this to Command history
					if (m_incompleteTyping.Length > 0)
					{
						CommandHistory.Add(m_incompleteTyping);
						m_lastEvent = ClientEvent.UserCommandEntered;
					}
					else
					{
						m_lastEvent = ClientEvent.IgnorableUserCommandEntered;						
					}

					
					return m_incompleteTyping;
			}

			return "";
		}

		public void ClearConsole(string command)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new String(' ', Console.BufferWidth - m_prompt.Length));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(m_prompt + command);
        }
	}

}
