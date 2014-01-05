using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynDNS
{
    public class Usuario
    {
        private string user;

        public string User
        {
            get { return user; }
            set { user = value; }
        }
        private string pass;

        public string Pass
        {
            get { return pass; }
            set { pass = value; }
        }
        private string ip;

        public string IP
        {
            get { return ip; }
            set { ip = value; }
        }
    }
}
