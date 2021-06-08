﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Link
{
    public class SecondThreadSend
    {
        private Semaphore _sendSemaphore;
        private Semaphore _receiveSemaphore;
        private PostToSecondRecieveWT _post;
        private BitArray _receivedMessage;
        private readonly Random random = new Random();


        public SecondThreadSend(ref Semaphore sendSemaphore, ref Semaphore receiveSemaphore)
        {
            _sendSemaphore = sendSemaphore;
            _receiveSemaphore = receiveSemaphore;
        }
        public void SecondThreadMain(object obj)
        {
            _post = (PostToSecondRecieveWT)obj;
            _post(new BitArray(StaticFunction.SerializeObject(AddSpecialPack(StaticFunction.RIM))));
            _sendSemaphore.Release();
            ConsoleHelper.WriteToConsole("3 поток", $"Отправлен запрос на соединение.");
            _receiveSemaphore.WaitOne();

            SetData();
        }

        public void ReceiveData(BitArray data)
        {
            _receivedMessage = data;
        }

        private void SetData()
        {
            Thread.Sleep(200);
            Receipt item = (Receipt)StaticFunction.DeserializeObject(StaticFunction.BitArrayToByteArray(_receivedMessage));
            PackWindow packWindow = null;
            if (item != null)
            {
                switch (BitConverter.ToInt32(StaticFunction.BitArrayToByteArray(item.Status), 0))
                {
                    case 1: //RR
                        packWindow = AddPack(item, null);
                        break;
                    case 3: //REJ
                        packWindow = AddPack(item, StaticFunction.Index);
                        break;
                    case 5: //DISC
                        ConsoleHelper.WriteToConsole("3 поток", "Закрытие соединения. Завершение работы.");
                        break;
                    case 7: //SIM
                        packWindow = AddSpecialPack(StaticFunction.UP);
                        ConsoleHelper.WriteToConsole("3 поток", "Отправлен запрос на передачу данных. Жду подтверждения.");
                        break;
                    case 9: //UA
                        packWindow = AddPack(item, null);
                        break;
                    default:
                        break;
                }
            }
            else
            {               
                ConsoleHelper.WriteToConsole("3 поток", "Нет квитанции. Отправляю снова.");
                packWindow = AddPack(item, StaticFunction.Index);
            }
            if (packWindow != null)
            {
                var randomNum = random.Next(1, 100);
                if (randomNum > 10)
                {
                    _post(new BitArray(StaticFunction.SerializeObject(packWindow)));

                    _sendSemaphore.Release();
                    _receiveSemaphore.WaitOne();

                    SetData();
                }
                else
                {
                   if (BitConverter.ToInt32(StaticFunction.BitArrayToByteArray(item.Status), 0) == StaticFunction.RR)
                   {
                       _receivedMessage = null;
                       SetData();
                   }
                   else
                   {
                        _post(StaticFunction.SetNoiseRandom(new BitArray(StaticFunction.SerializeObject(packWindow))));
                        _sendSemaphore.Release();
                        _receiveSemaphore.WaitOne();
                        SetData();
                   }
                }
            }

        }
        public int CheckSum(bool[] array)
        {
            int checkSum = 0;
            for (int p = 0; p < array.Length; p++)
            {
                checkSum += array[p] == false ? 0 : 1;
            }
            return checkSum;
        }

        public PackWindow AddPack(Receipt item, int? repeatId)
        {
            PackWindow packWindow = new PackWindow(0);
            if (repeatId == null)
            {
                while (StaticFunction.Index < StaticFunction.Data.Length)
                {
                    packWindow.Packs.Add(new Pack(id: StaticFunction.IndexPack(),
                                    bitArray: new BitArray(StaticFunction.Data[StaticFunction.Index]),
                                    checkSum: CheckSum(StaticFunction.Data[StaticFunction.Index]),
                                    useful: StaticFunction.Data[StaticFunction.Index].Length,
                                    status: new BitArray(BitConverter.GetBytes(StaticFunction.RR)),
                                    repeatId: null));
                    StaticFunction.IncrementIndex();
                    if (packWindow.Packs.Count == 2)
                    {
                        ConsoleHelper.WriteToConsole("3 поток", $"Передано {StaticFunction.GetIdPack} окно");
                        packWindow.UsefulPack = 2;
                        break;
                    }

                }
                if (packWindow.Packs.Count < 2)
                {
                    if (packWindow.Packs.Count == 0)
                    {
                        ConsoleHelper.WriteToConsole("3 поток", "Передан запрос на разрыв соединения. Жду подтверждения.");
                        packWindow.Packs.Add(new Pack(status: new BitArray(BitConverter.GetBytes(StaticFunction.RD))));
                    }
                    packWindow.Packs.Add(new Pack(null));
                    packWindow.UsefulPack = 1;
                }

            }
            else
            {
                while (StaticFunction.Index < StaticFunction.Data.Length)
                {
                    packWindow.Packs.Add(new Pack(id: StaticFunction.IndexPack(),
                                    bitArray: new BitArray(StaticFunction.Data[StaticFunction.Index]),
                                    checkSum: CheckSum(StaticFunction.Data[StaticFunction.Index]),
                                    useful: StaticFunction.Data[StaticFunction.Index].Length,
                                    status: new BitArray(BitConverter.GetBytes(StaticFunction.RR)),
                                    repeatId: StaticFunction.Index));
                    StaticFunction.DecrementIndex();
                    if (packWindow.Packs.Count == 2)
                    {
                        ConsoleHelper.WriteToConsole("3 поток", $"Передано {StaticFunction.GetIdPack} окно");
                        packWindow.UsefulPack = 2;
                        break;
                    }
                }
                if (packWindow.Packs.Count < 2)
                {
                    if (packWindow.Packs.Count == 0)
                    {
                        ConsoleHelper.WriteToConsole("3 поток", "Передан запрос на разрыв соединения. Жду подтверждения.");
                        packWindow.Packs.Add(new Pack(status: new BitArray(BitConverter.GetBytes(StaticFunction.RD))));
                    }
                    packWindow.Packs.Add(new Pack(null));
                    packWindow.UsefulPack = 1;
                }
            }
            return packWindow;
        }

       
        public PackWindow AddSpecialPack(int type)
        {
            PackWindow packWindow = new PackWindow(1);
            packWindow.Packs.Add(new Pack(status: new BitArray(BitConverter.GetBytes(type))));
            packWindow.Packs.Add(new Pack(null));

            return packWindow;
        }
    }
}
