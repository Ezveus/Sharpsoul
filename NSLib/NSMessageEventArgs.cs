using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSLib {
    public class NSMessageEventArgs {
        public NSMessageEventArgs(string msg) {
            this.Message = msg;
        }

        public string Message { get; private set; }
    }
}
