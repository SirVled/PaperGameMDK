using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerPaper
{
    class Program
    {
        class Client
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public Socket socket { get; set; }
        }

        class Player
        {
            public Client user { get; set; }
            public int idGame {get; set;}
            public int playersGame { get; set; }
            public bool play { get; set; } 
        }

        class GamePlayer
        {
            public Player player { get; set; }
            public int colorPlayer { get; set; }
        }

        static Socket soket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        static List<Client> clients = new List<Client>();
        static List<Player> players = new List<Player>(); //Лист игроков которые ищут игру в данный момент
        static List<GamePlayer> playPlayers = new List<GamePlayer>(); //Лист игроков которые играют в данный момент

        public static int[] valueGame = new int[3] { 0, 0, 0 };

        public static int idGame = 0;

        static void Main(string[] args)
        {
            soket.Bind(new IPEndPoint(IPAddress.Any, 8005));
            soket.Listen(0);
            
            soket.BeginAccept(AceptCallback,null);

            Console.WriteLine("Хост на связе!");
            Console.ReadLine();
        }

        private static void AceptCallback(IAsyncResult ar)
        {
            Socket client = soket.EndAccept(ar);
            Console.WriteLine("Опа, новый чел");
            Thread thread = new Thread(CoorCallback);
            thread.Start(client);
       
            soket.BeginAccept(AceptCallback, null);
        }

        private static void CoorCallback(object obj)
        {
            Socket client = (Socket)obj;
            MemoryStream ms = new MemoryStream(new byte[256], 0, 256, true, true);                
            BinaryReader reader = new BinaryReader(ms);

            int nextID = 0;
            try
            {
                while (true)
                {
                    if (client.Connected)
                    {
                        client.Receive(ms.GetBuffer());
                        ms.Position = 0;
                        switch (reader.ReadInt32())
                        {
                            //Connect 0
                            case 0:
                                Connect(nextID, reader.ReadString(), client);
                                nextID++;
                                break;

                            //Disconnect 1
                            case 1:
                                Disconnect(client);
                                break;

                            //Find Game 2
                            case 2:
                                FindGame(reader.ReadInt32(), client);
                                break;

                            //Break Find Game 3
                            case 3:
                                BreakFindGame(client);
                                break;

                            //Complete Find Game 4  
                            case 4:
                                CompleteFindGame(reader.ReadInt32());
                                break;

                            //Game Play 5
                            case 5:
                                GamePlay(reader.ReadString(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadInt32(), client);
                                break;

                            //Just Skip Turn
                            case 6:
                                JustSkipTurn(reader.ReadInt32(),client);
                                break;
                        }
                    }    
                }            
            }
            catch { }
        }

        /// <summary>
        /// Пропуск хода
        /// </summary>
        /// <param name="idGame">ID игры</param>
        private static void JustSkipTurn(int idGame, Socket client)
        {
            foreach(var pl in players)
            {
                if(pl.idGame == idGame && pl.user.socket != client)
                {
                    byte[] buff = new byte[4];
                    buff[0] = Convert.ToByte(3);

                    pl.user.socket.Send(buff);
                }
            }
        }

        /// <summary>
        /// Процесс игры
        /// </summary>
        /// <param name="coorBlock">Координаты блока на который кликнули</param>
        /// <param name="Kost1">Кость 1</param>
        /// <param name="Kost2">Кость 2</param>
        /// <param name="client">Клиент</param>
        private static void GamePlay(string coorBlock, double Kost1, double Kost2, int idGame, Socket client)
        {
            foreach(var pl in playPlayers)
            {
                if(pl.player.idGame == idGame && pl.player.user.socket != client)
                {
                  //  if(pl.player.user.socket != client)
                   // {
                        MemoryStream ms = new MemoryStream(new byte[256], 0, 256, true, true);
                        BinaryWriter writer = new BinaryWriter(ms);

                        writer.Write(2);
                        writer.Write(coorBlock);
                        writer.Write(Kost1);
                        writer.Write(Kost2);

                        pl.player.user.socket.Send(ms.GetBuffer());
                    //}
                }
            }
        }

      
        /// <summary>
        /// Создание новой игры
        /// </summary>
        /// <param name="numberGame">Кол-во игроков которые учавствуют в игре</param>
        private static void CompleteFindGame(int numberGame)
        {
            int colorTemp = 0;
            foreach(var pl in players)
            {
                if(pl.playersGame == numberGame && !pl.play)
                {
                    pl.play = true;
                    
                    GamePlayer gamePlayer = new GamePlayer
                    {
                        player = pl,
                        colorPlayer = colorTemp
                    };
                    
                    playPlayers.Add(gamePlayer);

                    MemoryStream ms = new MemoryStream(new byte[128], 0, 128, true, true);
                    BinaryWriter writer = new BinaryWriter(ms);

                    writer.Write(1);

                    writer.Write(numberGame);
                    writer.Write(pl.idGame);
                    writer.Write(colorTemp);      
                                
                    pl.user.socket.Send(ms.GetBuffer());

                    colorTemp++;
                }
            }
        }

        /// <summary>
        /// Отмена игры
        /// </summary>
        /// <param name="client">Клиент который отменил игру</param>
        private static void BreakFindGame(Socket client)
        {
            foreach (var pl in players)
            {
                if (pl.user.socket == client)
                {                  
                    Console.WriteLine(pl.user.Name + " отменил поиск игроков");
                    valueGame[pl.playersGame - 2]--;
                    players.Remove(pl);
                    break;
                }
            }
        }

        /// <summary>
        /// Поиск игры
        /// </summary>
        /// <param name="numberGame">кол-во игроков для игры</param>
        /// <param name="client">клиент который ищет игру</param>
        private static void FindGame(int numberGame, Socket client)
        {
           foreach(var cl in clients)
           {
               if(cl.socket == client)
               {
                   Player player = new Player 
                   {
                       user = cl,
                       idGame = idGame,
                       playersGame = numberGame,
                       play = false
                   };

                   Console.WriteLine(cl.Name + " запустил поиск игры на " + numberGame + " игрока");
                   players.Add(player);              
                   break;
               }
           }

           valueGame[numberGame - 2]++;
           SendValueFindGame(numberGame,client);
        }

        /// <summary>
        /// Отправляем всем клиентам прогресс найденной игры
        /// </summary>
        /// <param name="numberGame">Номер игры (1x1 1x2 1x3)</param>
        /// <param name="client">Клиент который ищет игру</param>
        private static void SendValueFindGame(int numberGame, Socket client)
        {
            for (int i = 0; i < valueGame.Length;i ++ )
            {
                if (valueGame[i] > 2 + i)
                {
                    valueGame[i] = 0;                  
                }
            }

            if (valueGame[numberGame - 2] == numberGame)
                idGame++;

            foreach (var pl in players)
            {
                if (pl.playersGame == numberGame)
                {
                    MemoryStream ms = new MemoryStream(new byte[64], 0, 64, true, true);
                    BinaryWriter writer = new BinaryWriter(ms);

                    writer.Write(0);
                    writer.Write(valueGame[numberGame - 2]);
                    writer.Write(numberGame);

                    pl.user.socket.Send(ms.GetBuffer());
                }
            }
        }


        private static void Disconnect(Socket client)
        {
            foreach(var playPlayer in playPlayers)
            {
                if(playPlayer.player.user.socket == client)
                {
                    playPlayers.Remove(playPlayer);
                    break;
                }
            }

            foreach (var pl in players)
            {
                if (pl.user.socket == client)
                {
                    players.Remove(pl);
                    valueGame[pl.playersGame - 2]--;                    
                    break;
                }
            }

            foreach(var cl in clients)
            {
                if(cl.socket == client)
                {
                    clients.Remove(cl);
                    Console.WriteLine(cl.Name + " вышел!");
                    break;
                }
            }
       
        }

        private static void Connect(int id, string name, Socket client)
        {
            Client cl = new Client
            {
               ID = id,
               Name = name,
               socket = client
            };

            clients.Add(cl);
        }

    }
}


/*

private static void Online()
        {
            MemoryStream ms2 = new MemoryStream(new byte[256], 0, 256, true, true);
            BinaryWriter writer = new BinaryWriter(ms2);
            
            foreach (var cl in clients)
            {
                writer.Write(cl.Name);
            }

            foreach (var cl in clients)
            {
                cl.socket.Send(ms2.GetBuffer());
            }
        }
*/ //Онлайн
/*          
                    MemoryStream ms2 = new MemoryStream(new byte[256], 0, 256, true, true);
                    BinaryWriter writer = new BinaryWriter(ms2);
         
                    writer.Write(name + " ");
                    client.Send(ms2.GetBuffer());

                    writer.Close();
                    /*
                    foreach(var cl in clients)
                    {
                        if(cl != client)
                        {
                            ms2 = new MemoryStream(new byte[256], 0, 256, true, true);
                            writer = new BinaryWriter(ms2);

                            writer.Write(name);

                            cl.Send(ms2.GetBuffer());
                        }
                    }
                     * */ //Отправка клиентам