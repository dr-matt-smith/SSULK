/**
 * @TODO: test this solution - seems to fix 'ghost' connetions issue (by not registering SocketAsyncEventArgs obejcts for SendOnce messages each time Console Log message is sent)
 * @TODO: sendone should send the DETAILED Console Log object ToString - with IP address and Timestamp etc.
 */


// source 
// https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs?view=netframework-4.7.2

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading; 
using System.Collections.Generic;
using System.ServiceModel.Channels;
using UnityEngine.Networking;

class Server : MonoBehaviour
{
    public LuaSimulator luaSimulator;
    internal class AsyncUserToken
    {
        public System.Net.Sockets.Socket Socket { get; set; }
    }
    
    private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously 
    private int m_receiveBufferSize;// buffer size to use for each socket I/O operation 
    BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
    const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
    Socket listenSocket;            // the socket used to listen for incoming connection requests
    // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
    SocketAsyncEventArgsPool m_readWritePool;
    int m_totalBytesRead;           // counter of the total # bytes received by the server
    int m_numConnectedSockets;      // the total number of clients connected to the server 
    Semaphore m_maxNumberAcceptedClients;

    private BufferHelper m_bufferHelper;

    private bool shutDown = false;

    private List<Socket> activeClientSockets = new List<Socket>();

    // Create an uninitialized server instance.  
    // To start the server listening for connection requests
    // call the Init method followed by Start method 
    //
    // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
    // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
    public void Construct(int numConnections, int receiveBufferSize)
    {
        m_totalBytesRead = 0;
        m_numConnectedSockets = 0;
        m_numConnections = numConnections;
        m_receiveBufferSize = receiveBufferSize;
        // allocate buffers such that the maximum number of sockets can have one outstanding read and 
        //write posted to the socket simultaneously  
        m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreAlloc, receiveBufferSize);
  
        m_readWritePool = new SocketAsyncEventArgsPool(numConnections);
        m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);

        m_bufferHelper = new BufferHelper(m_receiveBufferSize);
    }

    // Initializes the server by preallocating reusable buffers and 
    // context objects.  These objects do not need to be preallocated 
    // or reused, but it is done this way to illustrate how the API can 
    // easily be used to create reusable objects to increase server performance.
    //
    public void Init()
    {
        // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds 
        // against memory fragmentation
        m_bufferManager.InitBuffer();

        // preallocate pool of SocketAsyncEventArgs objects
        SocketAsyncEventArgs readWriteEventArg;

        for (int i = 0; i < m_numConnections; i++)
        {
            //Pre-allocate a set of reusable SocketAsyncEventArgs
            readWriteEventArg = new SocketAsyncEventArgs();
            readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            readWriteEventArg.UserToken = new AsyncUserToken();

            // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
            m_bufferManager.SetBuffer(readWriteEventArg);

            // add SocketAsyncEventArg to the pool
            m_readWritePool.Push(readWriteEventArg);
        }

    }

    // Starts the server such that it is listening for 
    // incoming connection requests.    
    //
    // <param name="localEndPoint">The endpoint which the server will listening 
    // for connection requests on</param>
    public void StartServer(IPEndPoint localEndPoint)
    {
        // create the socket which listens for incoming connections
        listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(localEndPoint);
        // start the server with a listen backlog of 100 connections
        listenSocket.Listen(100);
        
        Debug.Log("SERVER started and listening for connections from clients");
        Debug.Log("There are " + m_numConnectedSockets + " clients connected to the server");
        
        // post accepts on the listening socket
        StartAccept(null);            
    }
    
    // close connections and free up buffers when this GameObject is destroyed 
    // when scene changes or game application terminated
    void OnDestroy()
    {
        shutDown = true;
        
        // stop listening for Unity Console LOG events
        Application.logMessageReceivedThreaded -= HandleLog;
        
        // clear buffers for pool of Asych Args
        while (m_readWritePool.Count > 0)
        {
            SocketAsyncEventArgs args = m_readWritePool.Pop();
            // process value
            
            args.SetBuffer(null, 0, 0);
        }
        
        activeClientSockets.ForEach(socket =>
        {
            // try to shutdown this active socket
            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            
            SafeCloseSocket(socket);
        });
        
        // close the listening socket
        SafeCloseSocket(listenSocket);
        
        Debug.Log("There are " +  m_numConnectedSockets + " clients connected to the server");
        
        Debug.Log("server ended and sockets closed etc.");
    }

    /*
     * check socket is still connected before trying to close :-)
    */
    private void SafeCloseSocket(Socket s)
    {
        // close the listening socket
        if (s.Poll(10, SelectMode.SelectRead))
        {
            if (s.Connected)
                s.Close();
        }
    }


    // Begins an operation to accept a connection request from the client 
    //
    // <param name="acceptEventArg">The context object to use when issuing 
    // the accept operation on the server's listening socket</param>
    public void StartAccept(SocketAsyncEventArgs acceptEventArg)
    {
        // if shutting down, then exit - don't create a new 
        if (shutDown)
            return;
        
        if (acceptEventArg == null)
        {
            acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
        }
        else
        {
            // socket must be cleared since the context object is being reused
            acceptEventArg.AcceptSocket = null;
        }

        m_maxNumberAcceptedClients.WaitOne();
        bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
        if (!willRaiseEvent)
        {
            ProcessAccept(acceptEventArg);
        }
    }

    // This method is the callback method associated with Socket.AcceptAsync 
    // operations and is invoked when an accept operation is complete
    //
    void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
    {
        ProcessAccept(e);
    }

    private void ProcessAccept(SocketAsyncEventArgs e)
    {
        // if shutting down, then exit - don't create a new 
        if (shutDown)
            return;

        Interlocked.Increment(ref m_numConnectedSockets);
        Debug.Log("Client connection accepted. There are " + m_numConnectedSockets + " clients connected to the server");

        // Get the socket for the accepted client connection and put it into the 
        //ReadEventArg object user token
        SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
        ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

        // add 'e' from list of active clients
        activeClientSockets.Add(e.AcceptSocket);
        
        // As soon as the client is connected, post a receive to the connection
        bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
        if(!willRaiseEvent){
            ProcessReceive(readEventArgs);
        }

        // Accept the next connection request
        StartAccept(e);
    }
    
    //dfg
    
    

    // This method is called whenever a receive or send operation is completed on a socket 
    //
    // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
    void IO_Completed(object sender, SocketAsyncEventArgs e)
    {
        // determine which type of operation just completed and call the associated handler
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceive(e);
                break;
            case SocketAsyncOperation.Send:
                ProcessSend(e);
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a receive or send");
        }       

    }
    
    // This method is invoked when an asynchronous receive operation completes. 
    // If the remote host closed the connection, then the socket is closed.  
    // If data was received then the data is echoed back to the client.
    //
    private void ProcessReceive(SocketAsyncEventArgs e)
    {
        // check if the remote host closed the connection
        AsyncUserToken token = (AsyncUserToken)e.UserToken;
        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            
            
            //increment the count of the total bytes receive by the server
            Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);

            string msg = m_bufferHelper.GetMessageInBuffer(e);
//            Debug.Log("(1) received: '" + msg + "' : The server has read a total of " + m_totalBytesRead + " bytes");

            ProcessClientMessage(e, token, msg);
        }
        else
        {
            CloseClientSocket(e);
        }
    }

    ///
    /// received message from client
    /// process it and send some reply back to the client

    private void ProcessClientMessage(SocketAsyncEventArgs e, AsyncUserToken token, string messageFromClient)
    {
        string messageToClient = MessageToReturn(messageFromClient);
        m_bufferHelper.SetMessage(e, messageToClient);
        bool willRaiseEvent = token.Socket.SendAsync(e);
        if (!willRaiseEvent)
        {
            ProcessSend(e);
        }

    }

    /// 
    /// given message received from client
    /// decide what should be message to send back to them
    /// (default - output from LuaSimulator)
    /// 
    private string MessageToReturn(string clientMessage)
    {
        // special messages to server to be processed here
        switch (clientMessage)
        {
            case "hello":
                return "hello from the game server";
        }
		
        string resultMessage = luaSimulator.Execute(clientMessage);
        return resultMessage;
    }
    

    // This method is invoked when an asynchronous send operation completes.  
    // The method issues another receive on the socket to read any additional 
    // data sent from the client
    //
    // <param name="e"></param>
    private void ProcessSend(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            // done echoing data back to the client
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            // read the next block of data send from the client
            bool willRaiseEvent = token.Socket.ReceiveAsync(e);
            if (!willRaiseEvent)
            {
                ProcessReceive(e);
            }
        }
        else
        {
            CloseClientSocket(e);
        }
    }

    private void CloseClientSocket(SocketAsyncEventArgs e)
    {
        AsyncUserToken token = e.UserToken as AsyncUserToken;
        activeClientSockets.Remove(token.Socket);

        // close the socket associated with the client
        try
        {
            token.Socket.Shutdown(SocketShutdown.Send);
        }
        // throws if client process has already closed
        catch (Exception) { }
        token.Socket.Close();

        // decrement the counter keeping track of the total number of clients connected to the server
        Interlocked.Decrement(ref m_numConnectedSockets);


        // Free the SocketAsyncEventArg so they can be reused by another client
        m_readWritePool.Push(e);
        
        m_maxNumberAcceptedClients.Release();
        
        Debug.Log("A client has been disconnected from the server. There are " +  m_numConnectedSockets + " clients connected to the server");
    }


    private void SendOnce(Socket socket, string message)
    {
        //Pre-allocate a set of reusable SocketAsyncEventArgs
        SocketAsyncEventArgs sendOnceArgs = new SocketAsyncEventArgs();
        
        // do NOT register IO_Completed as callback for this send
//        sendOnceArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

        // create new buffer for this SocketAsyncEventArgs object
        sendOnceArgs.SetBuffer(new byte[m_receiveBufferSize], 0, m_receiveBufferSize);
        
        m_bufferHelper.SetMessage(sendOnceArgs, message);
        
        // copy socket from e.UserToken to our sendOnceArgs object
        AsyncUserToken token = new AsyncUserToken();
        sendOnceArgs.UserToken = token;

        // SEND message to Client
        token.Socket = socket;
        token.Socket.SendAsync(sendOnceArgs);
         
        
    }

    ///
    /// send the message to all connected clients
    ///
    private void BroadcastToAllConnectedClients(string message)
    {
        activeClientSockets.ForEach(
            socket => {
                SendOnce(socket, message);
            }
       );
    }

    
    ///
    /// LISTEN FOR Log messages
    ///
    void OnEnable() {	Application.logMessageReceivedThreaded += HandleLog; }

    void OnDisable() {	Application.logMessageReceivedThreaded -= HandleLog;	}

    void HandleLog(string logString, string stackTrace, LogType type) {
        if (activeClientSockets.Count > 0)
        {
            string message = "UnityConsole[" + type + "] " + logString;
            BroadcastToAllConnectedClients(message);            
        }
    }
}
