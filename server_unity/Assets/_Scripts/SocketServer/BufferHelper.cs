using System.Collections;
using System.Collections.Generic;

using System;
using System.Net.Sockets;
using System.Text; 


public class BufferHelper
{

    public  String GetMessageInBuffer(SocketAsyncEventArgs asyncEvent)
    {
        byte[] buffer = asyncEvent.Buffer;
        int offset = asyncEvent.Offset;
        int length = asyncEvent.BytesTransferred;
        
        return Encoding.UTF8.GetString(buffer, offset, length);
    }

       
    public void SetMessage(SocketAsyncEventArgs asyncEvent, string msg, int bufferSize)
    {
        int sendBufferLength = bufferSize;
        if (msg.Length < bufferSize)
        {
            sendBufferLength = msg.Length;
        }
        
        // set length of buffer for Aynch Event object
        asyncEvent.SetBuffer(asyncEvent.Offset, sendBufferLength);

        
        // create buffer, at least as long as buffer size, based on string
        byte[] tempBuffer = Encoding.ASCII.GetBytes(msg);
        
        // copy from temp buffer into buffer for event args object        
        Buffer.BlockCopy(tempBuffer, 0, asyncEvent.Buffer, asyncEvent.Offset, sendBufferLength);
    }


}
