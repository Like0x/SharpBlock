using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpBlock
{
    class PipeServer
    {

        private string PipeName = "";

        private static volatile int RuningThreadID = 0;
        private int MaxThread;
        private static volatile int SectionCount = 0;

        private static NamedPipeServerStream[] ServerGroup;

        private StringBuilder PipeContentStream = new StringBuilder();

        public static readonly object _locker = new object();

        public PipeServer(int MaxThread, String PipeName)
        {
            this.MaxThread = MaxThread;
            ServerGroup = new NamedPipeServerStream[MaxThread];
            this.PipeName = PipeName;
        }

        public void DisposePipe()
        {
            if (ServerGroup != null)
            {
                foreach (NamedPipeServerStream item in ServerGroup)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch (NullReferenceException e)
                    {

                    }

                }
            }
        }


        public void StartServers()
        {
            ArrayList ThreadArr = new ArrayList();
            for (int i = 0; i < this.MaxThread; i++)
            {
                Thread thread = new Thread(new ThreadStart(StartNewPipeServer));
                thread.Start();
                ThreadArr.Add(thread);
            }
            foreach (Thread t in ThreadArr)
            {
                t.Join();
            }
        }

        public StringBuilder GetContent()
        {
            return this.PipeContentStream;
        }

        private void StartNewPipeServer()
        {
            NamedPipeServerStream stream = null;
            stream = ServerGroup[RuningThreadID] = new NamedPipeServerStream(
                 this.PipeName,
                 PipeDirection.In,
                 this.MaxThread
                 );

            RuningThreadID += 1;
            StreamReader sr = new StreamReader(stream);

            while (true)
            {
                stream.WaitForConnection();
                String sResult = sr.ReadToEnd();

                if (sResult == "ok")
                {
                    break;
                }
                else
                {
                    lock (_locker)
                    {
                        Console.WriteLine($"[+] PipeStream {SectionCount} {sResult.Length} {sResult[0]}");
                        this.PipeContentStream.Append(sResult);
                        SectionCount++;
                    }

                }
                stream.Disconnect();

            }
        }

    }
}
