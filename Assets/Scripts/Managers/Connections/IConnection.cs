using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAR
{
    public delegate void MessageHandler(string message);

    /// <summary>
    /// Interface of a connection, each with a name, a connected flag
    /// When a message arrives, message handler will be called
    /// </summary>
    public interface IConnection
    {
        string Name { get; }
        bool Connected { get; }
        string StatusInfo { get; }

        event MessageHandler OnMessageReceived;

        void Start();
        void Update();

    }
}
