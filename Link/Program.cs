using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Link
{
    public delegate void PostToFirstSendWT(BitArray message);
    public delegate void PostToFirstRecieveWT(BitArray message);
    public delegate void PostToSecondSendWT(BitArray message);
    public delegate void PostToSecondRecieveWT(BitArray message);

    public class Program
    {
        
        static void Main(string[] args)
        {
            ConsoleHelper.WriteToConsole("Главный поток", "Введите 1 для ввода сообщения или 2 для файла");
            var num = int.Parse(Console.ReadLine());
            string data = null;           
            switch(num)
            {
                case 1:
                    ConsoleHelper.WriteToConsole("Главный поток", "Введите сообщение");
                    data = Console.ReadLine();
                    break;
                case 2:
                    ConsoleHelper.WriteToConsole("Главный поток", "Введите название файла");
                    data = Console.ReadLine();
                    break;
            }
            Encoding encoding = Encoding.UTF8;
            
            Semaphore firstReceiveSemaphore = new Semaphore(0, 1);
            Semaphore firstSendSemaphore = new Semaphore(0, 1);
            Semaphore secondReceiveSemaphore = new Semaphore(0, 1);
            Semaphore secondSendSemaphore = new Semaphore(0, 1);

            FirstThreadSend firstThreadSend = new FirstThreadSend(ref firstReceiveSemaphore, ref firstSendSemaphore);
            FirstThreadRecieve firstThreadRecieve = new FirstThreadRecieve(ref firstSendSemaphore, ref firstReceiveSemaphore);
            SecondThreadSend secondThreadSend = new SecondThreadSend(ref secondReceiveSemaphore, ref secondSendSemaphore);
            SecondThreadRecieve secondThreadRecieve = new SecondThreadRecieve(ref secondSendSemaphore, ref secondReceiveSemaphore);
            
            Thread threadFirstSend = new Thread(new ParameterizedThreadStart(firstThreadSend.FirstThreadMain));
            Thread threadFirstRecieve = new Thread(new ParameterizedThreadStart(firstThreadRecieve.FirstThreadMain));
            Thread threadSecondSend = new Thread(new ParameterizedThreadStart(secondThreadSend.SecondThreadMain));
            Thread threadSecondRecieve = new Thread(new ParameterizedThreadStart(secondThreadRecieve.SecondThreadMain));

            PostToFirstSendWT postToFirstSendWt = new PostToFirstSendWT(firstThreadSend.ReceiveData);
            PostToFirstRecieveWT postToFirstRecieveWt = new PostToFirstRecieveWT(firstThreadRecieve.ReceiveData);
            PostToSecondSendWT postToSecondSendWt = new PostToSecondSendWT(secondThreadSend.ReceiveData);
            PostToSecondRecieveWT postToSecondRecieveWt = new PostToSecondRecieveWT(secondThreadRecieve.ReceiveData);
            
            var serializeMessage = Task.Factory.StartNew(() =>
            {
                switch (num) {
                    case 1:                        
                        var bitArray = new BitArray(encoding.GetBytes(data));
                        var value = new bool[bitArray.Count];
                        for (int m = 0; m < bitArray.Count; m++)
                            value[m] = bitArray[m];
                        int j = 0;
                        StaticFunction.Data = value.GroupBy(s => j++ / StaticFunction.PackLength).Select(g => g.ToArray()).ToArray();
                        break;
                    case 2:
                        StaticFunction.SerializeFile(data);
                        break;
                }
            });

            threadFirstSend.Start(postToFirstRecieveWt);
            threadFirstRecieve.Start(postToFirstSendWt);
            threadSecondSend.Start(postToSecondRecieveWt);
            threadSecondRecieve.Start(postToSecondSendWt);

            Console.ReadLine();
        }
    }
}
