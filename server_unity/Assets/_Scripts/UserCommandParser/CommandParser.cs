
using UnityEngine;
using System;

class CommandParser : MonoBehaviour
{
    public LuaSimulator luaSimulator;
    private ConsoleHistory m_consoleHistory;

    void Awake()
    {
        // cache reference to ConsoleHistory 
        m_consoleHistory = GetComponent<ConsoleHistory>();
    }

    /// 
    /// given message received from client
    /// decide what should be message to send back to them
    /// (default - output from LuaSimulator)
    /// 
    public string MessageToReturn(string clientMessage)
    {
        string[] words = clientMessage.Split(' ');

        if(words.Length < 1)
            return "(no command received)";

        // one word commands
        if(1 == words.Length){
            switch(words[0]){
                case "hello":
                    return "hello from the game server";
                case "logs":
                    return m_consoleHistory.GetHistory();
                case "emptylogs":
                case "clearlogs":
                    return m_consoleHistory.Reset();
            }           
        }

        // two word commadns
        if(2 == words.Length){
            if("log" == words[0]){
                // 3rd words must evaluate to an integer
                int logId;
                if (Int32.TryParse(words[1], out logId))
                {
                    // seems okay - so set values ready to attempt to connect
                    return  m_consoleHistory.GetItemDetails(logId); 
                } else {
                    return "bad command - usage: log <n> (where <n> needs to be a valid log Id";
                }
            }
        }

        // otherwise pass on to Lua ...
        string resultMessage = luaSimulator.Execute(clientMessage);
        return resultMessage;
    }

}
