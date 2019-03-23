/*
 * TODO
 * - detect curretn IP address
 * - how to choose port
 */

using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class ServerManager : MonoBehaviour
{
	private Server server;

	private int maxConnections = 3;
	
	/*
	 * what is a good size for this - size of Socket messages
	 * - simple messages
	 * - LUA commands
	 * - reponse from LUA
	 * - Unity Log messages
	 */
	private int bufferSize = 2000;
		
	private string ipAddress = "127.0.0.1";
	private int port = 11000;
	
	// Use this for initialization
	void Awake ()
	{

		ipAddress = ServerManager.GetLocalIPAddress();
		
		server = GetComponent<Server>();

		Debug.Log("Starting Server: ipaddress = " + ipAddress + ", port = " + port);
		
		IPEndPoint ipEndpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
		
		server.Construct(maxConnections, bufferSize);
		server.Init();
		server.StartServer(ipEndpoint);
	}

	public static string GetLocalIPAddress()
	{
		var host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				return ip.ToString();
			}
		}
		throw new Exception("No network adapters with an IPv4 address in the system!");
	}

}
