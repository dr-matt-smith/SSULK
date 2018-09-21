namespace ConsoleApp2
{
    /*
     * structure to represent:
     * - if was a valid command to connect
     * - if valid, the IP address
     * - if valid, the PORT
     *
     * for testing if user entered something in the form:
     *     connect <ip> <port>
     *
     */
    public class ConnectCommandParseResult
    {
        public bool Valid = false;
        public string IpAddress;
        public int Port;
    }
}