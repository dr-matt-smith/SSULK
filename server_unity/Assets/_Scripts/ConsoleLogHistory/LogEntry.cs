/*
 * TODO
 * // got for SYSTEM LEVEL time 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogEntry 
{
	private string m_text;
	private string m_stackTrace;
	private string m_type;
	public string m_timestamp;

	public string m_ipAddress;

	public LogEntry(string text, string stackTrace, LogType type)
	{
		m_text = text;
		m_stackTrace = stackTrace;
		m_type = type.ToString();

		DateTime timestamp = DateTime.Now;
		m_timestamp = String.Format("{0:u}", timestamp);

		m_ipAddress = ServerManager.GetLocalIPAddress();

	}

	public override string ToString()
	{
		string s = m_ipAddress + " @ " + m_timestamp + " [" + m_type + "] " + m_text;
		return s;
	}

	public string Details()
	{
		string s = this.ToString();
		s += "\n";
		s += m_stackTrace;
		return s;
	}



}
