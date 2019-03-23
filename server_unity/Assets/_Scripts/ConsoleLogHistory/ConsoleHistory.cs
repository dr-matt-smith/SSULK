using System.Collections.Generic;
using UnityEngine;

public class ConsoleHistory : MonoBehaviour 
{
	private List<LogEntry> m_history = new List<LogEntry>();

	public string GetHistory()
	{
		if(m_history.Count < 1)
			return "(the log of Unity Console messages is empty)";

		string log = "";

		// for (int i = 0; i < history.Count; i++)
        // {
		// 	log += i + " = " + history[i] + "\n";
        // }

		// most recent first sequence seems most useful ...
		for (int i = m_history.Count - 1; i >= 0; i--)
        {
			log += i + " = " + m_history[i] + "\n";
        }

		return log;
	}

	public string GetItemDetails(int i){
		if((i < m_history.Count) && (i > -1)){
			return i + " = " + m_history[i].Details();
		} else {
			return "sorry - no item " + i + " in Console history log";
		}
	}

	public string Reset()
	{
		m_history = new List<LogEntry>();
		return "(all Unity Console logs cleared from memory)";
	}

    ///
    /// LISTEN FOR Log messages
    ///
    void OnEnable() {	Application.logMessageReceivedThreaded += HandleLog; }

    void OnDisable() {	Application.logMessageReceivedThreaded -= HandleLog;	}
    void OnDestroy() {	Application.logMessageReceivedThreaded -= HandleLog;	}

    void HandleLog(string logString, string stackTrace, LogType type) 
	{
		LogEntry logEntry = new LogEntry(logString, stackTrace, type);
		m_history.Add(logEntry);            
    }


}
