using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThunderTank
{
    public class Network
    {
        private const int _Port = 2110;

        public void CreateRoom()
        {

            var ipendPoint = new IPEndPoint(0,_Port);
        }
    }
}
