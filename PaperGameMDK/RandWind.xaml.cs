using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PaperGameMDK
{
    /// <summary>
    /// Interaction logic for RandWind.xaml
    /// </summary>
    public partial class RandWind : Window
    {
        public RandWind()
        {
            InitializeComponent();
        }

        static Socket soket;
        static MemoryStream ms;
        static BinaryWriter writer;
        static bool PlayOrExit = true;
        Thread thread;

        private void Start(object sender, RoutedEventArgs e)
        {
            findGameGrid.Visibility = Visibility.Hidden;
            PanelUser.IsEnabled = false;
        }

        private void ConnectBT(object sender, RoutedEventArgs e)
        {

            if ((sender as Button).Content.Equals("Connect"))
            {
                (sender as Button).Content = "Disconnected";
                Connected();

                nameUserTB.IsEnabled = false;

                PanelUser.IsEnabled = true;
            }
            else
            {
                (sender as Button).Content = "Connect";
                Disconnected(soket);

                nameUserTB.IsEnabled = true;
                PanelUser.IsEnabled = false;
            }

            /*
            ms = new MemoryStream(new byte[256], 0, 256, true, true);
            writer = new BinaryWriter(ms);
             * */
        }

        /// <summary>
        /// Отключение от сервера.
        /// </summary>
        public void Disconnected(Socket soket)
        {
            findGameGrid.Visibility = Visibility.Hidden;

            if (thread != null)
                thread.Abort();

            if (PlayOrExit)
            {
                try
                {
                    byte[] buff = new byte[4];
                    buff[0] = Convert.ToByte(1);
                    soket.Send(buff);
                    soket.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// Подключение к серверу.
        /// </summary>
        private void Connected()
        {

            int port = 8005; // порт сервера
            string address = "192.168.1.2"; // адрес сервера

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
            soket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            soket.Connect(ipPoint);

            ms = new MemoryStream(new byte[256], 0, 256, true, true);
            writer = new BinaryWriter(ms);

            thread = new Thread(Otvet);
            thread.ApartmentState = ApartmentState.STA;
            thread.Start(soket);
            
            writer.Write(0);
            writer.Write(nameUserTB.Text);

            soket.Send(ms.GetBuffer());
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">Closed</param>
        private void CloseWind(object sender, EventArgs e)
        {
            Disconnected(soket);
        }

        private void Otvet(object o)
        {       
            Socket soket = (Socket)o;
            MemoryStream ms = new MemoryStream(new byte[256], 0, 256, true, true);
            BinaryReader reader = new BinaryReader(ms);
        //    try
        //      {
                while (true)
                {
                
                    soket.Receive(ms.GetBuffer());
                    ms.Position = 0;

                    switch (reader.ReadInt32())
                    {
                        //Set Players Value Game 0
                        case 0:
                            SetPlayersValueGame(reader.ReadInt32(),reader.ReadInt32());
                            break;

                        //New Game 1
                        case 1:
                            CreateNewGame(reader.ReadInt32(),reader.ReadInt32(),reader.ReadInt32());
                            break;
                    }

                    if (!soket.Connected)
                    {                                  
                        break;
                    }
                  
                }
                      
        // }
        //  catch { }
        }

        private void CreateNewGame(int maxPlayers, int idGame, int colorPlayer)
        {
            Dispatcher.Invoke(new ThreadStart(() =>
            {
                RandWind randWind = this;
                MainWindow main = new MainWindow(maxPlayers, idGame, colorPlayer, soket,randWind);
                main.ShowDialog();
                PlayOrExit = false;
                Disconnected(soket);
                              
            }));
        }

        private void SetPlayersValueGame(int players, int maxPlayers)
        {
            //try
          //  {
            Dispatcher.Invoke(new ThreadStart(() =>
            {
                lbValuePlayers.Content = players + " / " + maxPlayers;
            }));

            if (players >= maxPlayers)
                {
                    CompleteFindGame(maxPlayers);                 
                }       
          //  }
           // catch { }
        }

        /// <summary>
        /// Отправка на сервер о успешном нахождении нужного количества людей для игры
        /// </summary>
        /// <param name="maxPlayers">Кол - во игроков</param>
        private void CompleteFindGame(int maxPlayers)
        {
            MemoryStream ms = new MemoryStream(new byte[64], 0, 64, true, true);
            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(4);
            writer.Write(maxPlayers);

            soket.Send(ms.GetBuffer());
        }

        /// <summary>
        /// Поиск игры
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Click</param>
        private void FindGame(object sender, RoutedEventArgs e)
        {
            findGameGrid.Visibility = Visibility.Visible;

            MemoryStream ms = new MemoryStream(new byte[32], 0, 32, true, true);
            BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(2);
            writer.Write(Int32.Parse((sender as Button).Content.ToString()));

            soket.Send(ms.GetBuffer());
        }

        /// <summary>
        /// Отмена поиска игры
        /// </summary>
        /// <param name="sender">Button</param>
        /// <param name="e">Click</param>
        private void BackToMenu(object sender, RoutedEventArgs e)
        {
            findGameGrid.Visibility = Visibility.Hidden;

            byte[] buff = new byte[4];
            buff[0] = Convert.ToByte(3);
           
            soket.Send(buff);
        }
    }
}

/*
                   string s = string.Empty;
                   while (ms.Position != ms.Length)
                   {
                       string ss = reader.ReadString();
                       if (ss != "")
                           s += ss;
                   }

                   if (s != string.Empty)
                   {
                       MessageBox.Show(s);
                   }
*/ // Получение данных от сервера