using System;
// using System.Collections.Generic;
// using System.Linq;
using System.Text;
// using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace NSLib
{
    /// <summary>
    /// Represents a NetSoul client
    /// </summary>
    public class NSClient {
        private TcpClient client;
        public event EventHandler<NSMessageEventArgs> newMessage;
        public event EventHandler<NSUserLogEventArgs> newUserLog;
        private NetworkStream stream;

        /// <summary>
        /// Defines status used during the connection
        /// </summary>
        public enum Status {
            Success,
            AuthAGErr,
            ExtUserLogErr,
            AttachErr
        }

        #region Constructors

        /// <summary>
        /// Initialize the internal TcpClient using a IPEndPoint
        /// </summary>
        /// <param name="endpoint">
        /// IPEndPoint : given to the TcpClient constructor : represents a distant host
        /// </param>
        public NSClient(IPEndPoint endpoint, NSUser user) {
            client = new TcpClient(endpoint);
            stream = client.GetStream();
            Status stat = initConnection(user.Login, user.Password, user.Data, user.Location);
            if (stat != Status.Success) {
                // throw Exception for the value of stat
            }
        }

        /// <summary>
        /// Initialize the internal TcpClient using hostname and port
        /// </summary>
        /// <param name="host">
        /// string : given to the TcpClient constructor : represents a distant host as DNS or IP
        /// </param>
        /// <param name="port">
        /// int : given to the TcpClient constructor : represents the distant port
        /// </param>
        public NSClient(string host, int port, NSUser user) {
            client = new TcpClient(host, port);
            stream = client.GetStream();
            Status stat = initConnection(user.Login, user.Password, user.Data, user.Location);
            if (stat != Status.Success) {
                throw new Exception("Status wasn't Success"); // throw Exception for the value of stat
            }
        }

        /// <summary>
        /// Common part of the various constructors
        /// It logs the user on the NS server
        /// </summary>
        /// <param name="userLogin">
        /// string : user login
        /// </param>
        /// <param name="userPassword">
        /// string : user socks password
        /// </param>
        /// <param name="userData">
        /// string : user defined data (64 characters long, should already be url encoded)
        /// </param>
        /// <param name="userLocation">
        /// string : user location (64 characters long, should already be url encoded)
        /// </param>
        /// <returns>
        /// Success or Failure
        /// </returns>
        private Status initConnection(string userLogin, string userPassword, 
            string userData, string userLocation) {
            userData = reduceAndEncode(userData);
            userLocation = reduceAndEncode(userLocation);
            // Read welcome message of the server : "salut <socket number> <random md5 hash> <client host> <client port> <server timestamp>"
            string welcomeStr = read();
            // Split this message according to spaces to get the dynamic informations ('<dynamic info>')
            string[] welcomeTab = welcomeStr.Split(' ');
            // Ask for authentication : "auth_ag ext_user none none\n"
            write("auth_ag ext_user none none\n");
            if (getAnswer() == false)
                return Status.AuthAGErr;
            // Create the md5 answer : getMd5String("<random md5 hash>-<client host>/<client port><socks password>")
            string md5Answer = getMd5String(String.Format("{0}-{1}/{2}{3}",
                welcomeTab[2], welcomeTab[3], welcomeTab[4], userPassword)); 
            // Authenticate : write("ext_user_log <login> <md5 answer> <user data> <user location>")
            write(String.Format("ext_user_log {0} {1} {2} {3}\n", userLogin, md5Answer, 
                userData, userLocation));
            if (getAnswer() == false)
                return Status.ExtUserLogErr;
            // Get access to all resources : writeCommand("attach")
            //write("attach");
            //if (getAnswer() == false)
            //    return Status.AttachErr;
            // Change status to active : setState("actif")
            setState("actif");
            return Status.Success;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Parse the answer
        /// </summary>
        /// <returns>
        /// Success or failure
        /// </returns>
        private bool getAnswer() {
            string answer = read();

            if (answer != "rep 002 -- cmd end\n")
                return false;
            return true;
        }

        /// <summary>
        /// Returns a md5 from a string
        /// </summary>
        /// <param name="str">
        /// string : source string
        /// </param>
        /// <returns>
        /// str as a md5 hash
        /// </returns>
        public static string getMd5String(string str) {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(str));
            StringBuilder sb = new StringBuilder();

            foreach (byte b in hash) {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get a utf8 string from the server
        /// </summary>
        /// <returns>
        /// String : what was read on the server
        /// </returns>
        private string read() {
            var data = new Byte[256];
            int bytes = stream.Read(data, 0, data.Length);

            return Encoding.ASCII.GetString(data, 0, bytes);
        }

        /// <summary>
        /// Send a utf8 string to the server
        /// </summary>
        /// <param name="str">
        /// string to send
        /// </param>
        private void write(string str) {
            Byte[] data = Encoding.ASCII.GetBytes(str);

            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Send a command to the server : write("user_cmd <command>")
        /// </summary>
        /// <param name="command">
        /// string : command to send to the server
        /// </param>
        private void writeCommand(string command) {
            write(String.Format("user_cmd {0}\n", command));
        }

        /// <summary>
        /// URL encode a string and reduce its size to 64
        /// </summary>
        /// <param name="str">
        /// string : input string
        /// </param>
        /// <returns>
        /// url encoded string of size 64
        /// </returns>
        public static string reduceAndEncode(string str) {
            String s = System.Web.HttpUtility.UrlEncode(str).Replace("+", "%20");
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 64 && i < s.Length; i++) {
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Find unix timestamp (seconds since 01/01/1970)
        /// </summary>
        /// <returns>
        /// string : unix timestamp
        /// </returns>
        public static string getUnixTimestamp() {
            TimeSpan ts = new TimeSpan(DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks);
            return ((int)ts.TotalSeconds).ToString();
        }
        #endregion

        #region Commands from the client

        // Change state : writeCommand("state <newState>:<timestamp>")
        public void setState(string newState) {
            writeCommand(String.Format("state {0}:{1}", newState, getUnixTimestamp()));
        }

        // Send a message to a client : writeCommand("msg_user <login> msg <message>")
        public void sendMessage(string login, string message) {
        }

        // Send a message to a list of clients : writeCommand("msg_user {<login[0]>,<login[1]>} msg <message>")
        public void sendMessage(string[] logins, string message) {
        }

        // Get the informations of a user : write("list_users <login>")
        public string[] getUserInfo(string login) {
            return new string[] { "login", "state", "location", "data" };
        }

        // Get the informations of a list of users : write("list_users {<login[0]>,<login[1]>}")
        public string[][] getUsersInfo(string[] logins) {
            return new string[][] { new string[] { "login", "state", "location", "data" }, new string[] { "login", "state", "location", "data" } };
        }

        // Exit from the server : write("exit")
        public void disconnect() {
            // this.Close();
        }

        // Watch the logs of a user : writeCommand("watch_log_user <login>")
        public void watchUser(string login) {
        }

        // Watch the logs of a list of users : writeCommand("watch_log_user {<login[0]>,<login[1]>}")
        public void watchUsers(string[] logins) {
        }

        #endregion

        #region Reception of messages from the server

        // Send an event notifying the arrival of a watch_log_user answer
        private void newUserLogComming() {
            this.newUserLog(this, new NSUserLogEventArgs());
        }

        // Send an event notifying the arrival of a new message
        private void newMessageComming(string message) {
            this.newMessage(this, new NSMessageEventArgs(message));
        }

        #endregion
    }
}