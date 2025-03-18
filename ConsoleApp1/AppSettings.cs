
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

public class AppSettings
{
    public string Rr12IpAddress { get; set; }
    public int Rr12Port { get; set; }
    public string Rr12TcpClient { get; set; }
    public int Rr12TcpPort { get; set; }

	public string RfidStreamIpAddress { get; set; }
    public int RfidStreamPort { get; set; }
    public int StatusUpdateInterval { get; set; } // in milliseconds
    public double ProtocolVersion { get; set; }
    public string deviceID { get; set; }
}
