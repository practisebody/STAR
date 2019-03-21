using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAR
{
    public delegate void MessageHandler(string message);

    public interface IConnection
    {
        string Name { get; }
        bool Connected { get; }

        event MessageHandler OnMessageReceived;

        void Start();
        void Update();

    }
}
