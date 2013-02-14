using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSLib {
    /// <summary>
    /// Represent the connected user
    /// </summary>
    public class NSUser {

        public string Login { get; private set; }
        public string Password { get; private set; }
        public string Data { get; private set; }
        public string Location { get; private set; }

        #region Constructors

        /// <summary>
        /// Build a NSUser
        /// </summary>
        /// <param name="login">
        /// string : login
        /// </param>
        /// <param name="password">
        /// string : socks password
        /// </param>
        /// <param name="data">
        /// string : user defined data
        /// </param>
        /// <param name="location">
        /// string : user location
        /// </param>
        public NSUser(string login, string password, string data, string location) {
            this.Login = login;
            this.Password = password;
            this.Data = data;
            this.Location = location;
        }
        #endregion
    }
}
