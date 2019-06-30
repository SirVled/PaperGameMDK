using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PaperGameMDK
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Random rnd = new Random();
        public static int Blocks = 40;
        public Rectangle[,] RecB = new Rectangle[Blocks,Blocks];

        private Rectangle rememberHoverRec { get; set; }

        private List<Rectangle> tryBlocks; // запоминаем блоки которые нужно очистить/заполнить

        private static int maxValPlayers = 3;/// Кол-во игроков
        private int swapTurn = 1;

        private Label[] scoresPlayers = new Label[maxValPlayers];

        private double Kost1 = 0;
        private double Kost2 = 0;

        private int idGame;
        private int colorPlayer;
        private Socket soket;

        private Thread thread;
        private RandWind randWind;
        private double RemKost1 { get; set; }
        private double RemKost2 { get; set; }


        public MainWindow()
        {
            InitializeComponent();
        }


        public MainWindow(int maxPlayers, int idGame, int colorPlayer, Socket soket,RandWind randWind)
        {
            
            this.idGame = idGame;
            this.colorPlayer = colorPlayer;         
            this.soket = soket;
            this.randWind = randWind;
            this.randWind.Hide();
            maxValPlayers = maxPlayers;

            scoresPlayers = new Label[maxValPlayers];
            
            InitializeComponent();
        }

        /// <summary>
        /// Старт игры!
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">Loaded</param>
        private void StartGame(object sender, RoutedEventArgs e)
        {

            for (int j = 0; j < Blocks; j++)
            {
                ColumnDefinition cd = new ColumnDefinition();
                GridGame.ColumnDefinitions.Add(cd);
                RowDefinition rd = new RowDefinition();
                GridGame.RowDefinitions.Add(rd);
            }

            for(int i = 0 ; i < Blocks; i++)
            {
                for (int j = 0; j < Blocks; j++)
                {
                    RecB[i, j] = createRec(i,j);

                    RecB[i, j].Tag = i + ":" + j;
                    GridGame.Children.Add(RecB[i, j]);

                    RecB[i,j].MouseLeftButtonDown += CreateTerritory;
                    RecB[i, j].MouseEnter += HoverBlock;
                }
            }

            CreateLabelBlockPlayers();
            CreateScorePlayers();
            SetColorTurns.Fill = ColorTurn();

            TurnL.Text = contentLab(swapTurn);
       
          //  LisenServer();
            RndKost();
        }

        /// <summary>
        /// Устанавливает background
        /// </summary>
        private SolidColorBrush ColorTurn()
        {
            SolidColorBrush br = null;
            switch(colorPlayer)
            {
                case 0:
                    br = new SolidColorBrush(Color.FromRgb(106, 139, 253));
                    return br;

                case 1:
                    br = new SolidColorBrush(Color.FromRgb(253, 106, 106));
                    return br;

                case 2:
                    br = new SolidColorBrush(Color.FromRgb(246, 253, 106));
                    return br;

                case 3:
                    br = new SolidColorBrush(Color.FromRgb(106, 253, 113));
                    return br;

                default:
                    return br;
            }
        }

        private void LisenServer()
        {
            thread = new Thread(Otvet);
            thread.ApartmentState = ApartmentState.STA;
            thread.Start(soket);
        }

        private void Otvet(object obj)
        {
            Socket soket = (Socket)obj;
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
                    // Coor New 2
                    case 2:
                        CoorNew(reader.ReadString(), reader.ReadDouble(), reader.ReadDouble());
                        break;
                    // Just Pass Turn 3
                    case 3:
                        Dispatcher.Invoke(new ThreadStart(() =>
                        {
                            NewTurn();
                        }));
                        break;
                }

                if (!soket.Connected)
                {
                    break;
                }

            }
        }
        
        private void CoorNew(string poz, double Kost1, double Kost2)
        {
            Dispatcher.Invoke(new ThreadStart(() =>
             {
                 string[] pozition = poz.Split(':');
                 object obj = RecB[Int32.Parse(pozition[0]),Int32.Parse(pozition[1])];
                 PlayersTurn(obj, Kost1, Kost2);            
             }));
        }

        /// <summary>
        /// Ход получений из сервера
        /// </summary>
        /// <param name="obj">Первый объект (Rectangle) в прямоугольнике</param>
        /// <param name="Kost1">Кость 1</param>
        /// <param name="Kost2">Кость 2</param>
        private void PlayersTurn(object obj, double Kost1, double Kost2)
        {
            Label setBlock = new Label
            {
                Background = TurnL.Background
            };

            string[] ss = TurnL.Text.Split(' ');

           
            
            var rec = (obj as Rectangle);

            for (int i = Grid.GetRow(rec); i < Grid.GetRow(rec) + Kost1; i++)
            {
                for (int j = Grid.GetColumn(rec); j < Grid.GetColumn(rec) + Kost2; j++)
                {

                    GridGame.Children.Remove(RecB[i, j]);
                    RecB[i, j].Tag = ss[0];
                   
                }
            }

            Grid.SetRow(setBlock, Grid.GetRow(rec));
            Grid.SetColumn(setBlock, Grid.GetColumn(rec));

            Grid.SetRowSpan(setBlock, (int)Kost1);
            Grid.SetColumnSpan(setBlock, (int)Kost2);

            GridGame.Children.Add(setBlock);

            ToUpdateScore(Kost1 * Kost2);
            NewTurn();          
        }

        /// <summary>
        /// Создает список очков игроков
        /// </summary>
        private void CreateScorePlayers()
        {
            for (int i = 1; i <= maxValPlayers; i++)
            {
                scoresPlayers[i - 1] = new Label
                    {
                        Content = "0",
                        Margin = new Thickness(5, 0, 0, 0),
                        FontSize = 12,
                        FontWeight = lbWeight.FontWeight
                    };
                scoresPlayers[i - 1].Foreground = ForegPlayer(i);

                ScorePanel.Children.Add(scoresPlayers[i - 1]);
            }         
        }

        /// <summary>
        /// Создает список очков игроков с своим цветом
        /// </summary>
        /// <param name="i">номер игрока</param>
        /// <returns>Цвет игрока</returns>
        private Brush ForegPlayer(int player)
        {
            switch (player)
            {
                case 1:
                    return Brushes.Blue;
                case 2:
                    return Brushes.Red;
                case 3:
                    return Brushes.Yellow;
                case 4:
                    return Brushes.Green;

                default :
                    return Brushes.Gray;
            }
        }

        /// <summary>
        /// Определение кол-во игроков и создание для них начальных блоков
        /// </summary>
        private void CreateLabelBlockPlayers()
        {
            for(int i = 1; i <= maxValPlayers; i++)
            {
                GridGame.Children.Add(CreateBlockPlayers(i));
            }           
        }

        /// <summary>
        /// Создание начальных блоков игроков
        /// </summary>
        /// <param name="player">Номер игрока</param>
        /// <returns>Блок (Label)</returns>
        private Label CreateBlockPlayers(int player)
        {

            switch (player)
            {
                case 1 :

                    Label blue = new Label { 
                    Background = Brushes.DarkBlue,
                    Tag = "Blue"
                    };

                Grid.SetColumnSpan(blue, 3);
                Grid.SetRowSpan(blue, 3);

                for(int i = 0; i < 3; i++)
                {
                    for(int j = 0; j < 3; j++)
                    {
                        GridGame.Children.Remove(RecB[i, j]);
                        RecB[i, j].Tag = "Blue";
                    }
                }
            return blue;

                case 2 :

                    Label red = new Label { 
                    Background = Brushes.DarkRed,
                    Tag = "Red"
                    };

                    Grid.SetColumnSpan(red, 3);
                    Grid.SetRowSpan(red, 3);

                    Grid.SetColumn(red, Blocks - 3);
                    Grid.SetRow(red, Blocks - 3);

                    for(int i = Blocks - 3; i < Blocks; i++)
                    {
                        for(int j = Blocks - 3; j < Blocks; j++)
                        {
                            GridGame.Children.Remove(RecB[i, j]);
                            RecB[i, j].Tag = "Red";
                        }
                    }
                    return red;

                case 3 :

                    Label yellow = new Label { 
                    Background = Brushes.Yellow,
                    Tag = "Yellow"
                    };

                    Grid.SetColumnSpan(yellow, 3);
                    Grid.SetRowSpan(yellow, 3);

                    

                    if (maxValPlayers > 3)
                    {
                        Grid.SetColumn(yellow, Blocks - 3);
                        Grid.SetRow(yellow, 0);

                        for (int i = 0; i < 3; i++)
                        {
                            for (int j = Blocks - 3; j < Blocks; j++)
                            {
                                GridGame.Children.Remove(RecB[i, j]);
                                RecB[i, j].Tag = "Yellow";
                            }
                        }
                    }
                    else
                    {

                        Grid.SetColumn(yellow, (Blocks / 2) - 1);
                        Grid.SetRow(yellow, (Blocks / 2) - 2);

                        for (int i = (Blocks / 2) - 2; i < (Blocks / 2) + 1; i++)
                        {
                            for (int j = (Blocks / 2) - 1; j < (Blocks / 2) + 2; j++)
                            {
                                GridGame.Children.Remove(RecB[i, j]);
                                RecB[i, j].Tag = "Yellow";
                            }
                        }
                    }
                    return yellow;

                case 4 :

                    Label green = new Label { 
                    Background = Brushes.Green,
                    Tag = "Green"
                    };

                    Grid.SetColumnSpan(green, 3);
                    Grid.SetRowSpan(green, 3);

                    Grid.SetColumn(green, 0);
                    Grid.SetRow(green, Blocks - 3);

                    for(int i = Blocks - 3; i < Blocks; i++)
                    {
                        for(int j = 0; j < 3; j++)
                        {
                            GridGame.Children.Remove(RecB[i, j]);
                            RecB[i, j].Tag = "Green";
                        }
                    }

                    return green;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Отображение блока который нужно поставить на поле
        /// </summary>
        /// <param name="sender">Rectangle</param>
        /// <param name="e">MouseEnter</param>
        private void HoverBlock(object sender, MouseEventArgs e)
        {

            var sen = sender as Rectangle;
            Hovers(sen);
        }

        /// <summary>
        /// Постройка Hover прямоугольника
        /// </summary>
        /// <param name="sen">Объект на который навелись</param>
        private void Hovers(Rectangle sen)
        {
            if (Grid.GetRow(sen) + Kost1 / 2 <= Blocks && Grid.GetColumn(sen) + Kost2 / 2 <= Blocks &&
                Grid.GetRow(sen) - Kost1 / 2 >= ((Kost1 % 2 == 0) ? 0 : -1) && Grid.GetColumn(sen) - Kost2 / 2 >= ((Kost2 % 2 == 0) ? 0 : -1))
            {
                if (tryBlocks != null)
                    HoverNull();
                //  Rectangle rec = RecB[Grid.GetRow(sen) - Kost1 / 2, Grid.GetColumn(sen) - Kost2 / 2];

                Boolean CheckValueBlock = true;

                tryBlocks = new List<Rectangle>();
                for (int i = Grid.GetRow(sen) - (int)Kost1 / 2; i < Grid.GetRow(sen) + Kost1 / 2; i++)
                {
                    for (int j = Grid.GetColumn(sen) - (int)Kost2 / 2; j < Grid.GetColumn(sen) + Kost2 / 2; j++)
                    {
                        try
                        {
                            /// Если нету места куда вставлять выбранный блок
                            if (!GridGame.Children.Contains(RecB[i, j]))
                                CheckValueBlock = false;
                            tryBlocks.Add(RecB[i, j]);
                        }
                        catch { }
                    }
                }

                if (CheckValueBlock)
                {
                    CheckValueBlock = SosedBlockColor(tryBlocks);
                }

                for (int i = Grid.GetRow(sen) - (int)Kost1 / 2; i < Grid.GetRow(sen) + Kost1 / 2; i++)
                {
                    for (int j = Grid.GetColumn(sen) - (int)Kost2 / 2; j < Grid.GetColumn(sen) + Kost2 / 2; j++)
                    {
                        try
                        {
                            if (CheckValueBlock)
                                RecB[i, j].Fill = Brushes.Blue;
                            else
                            {
                                RecB[i, j].Fill = Brushes.Red;
                            }

                        }
                        catch { }
                    }
                }
                for (int i = Grid.GetRow(sen) + (int)Kost1 / 2; i > Grid.GetRow(sen) - Kost1 / 2; i--)
                {
                    for (int j = Grid.GetColumn(sen) - (int)Kost2 / 2; j > Grid.GetColumn(sen) + Kost2 / 2; j--)
                    {
                        try
                        {
                            if (GridGame.Children.Contains(RecB[i, j]))
                                RecB[i, j].Fill = Brushes.Blue;
                        }
                        catch (Exception) { }
                    }
                }

                RemKost1 = Kost1;
                RemKost2 = Kost2;
                rememberHoverRec = sen;
            }
        }

        /// <summary>
        /// Проверка на то, есть ли рядом цвет блока нашего хода.
        /// </summary>
        /// <param name="Kost1">Кубик1</param>
        /// <param name="Kost2">Кубик2</param>
        /// <returns>Bool</returns>
        private bool SosedBlockColor(List<Rectangle> sen)
        {
            if (UpDownCheck(sen) || LeftRightCheck(sen))
                return true;

            return false;
        }

        /// <summary>
        /// Проверяет соседние (верхние - нижние) на наличие своей территории (т.е того цвета который сейчас ходит)
        /// </summary>   
        /// <param name="sen">Список выделенных прямоугольников</param>
        /// <returns>Можно ли ставить блок или нет</returns>
        private bool UpDownCheck(List<Rectangle> sen)
        {
            string[] ss = TurnL.Text.Split(' ');

            for (int i = 0; i < sen.Count; i++)
            {

                int a = Grid.GetRow(sen[i]);
                int b = Grid.GetColumn(sen[i]);

                //^\\
                if (a + 1 < Blocks)
                {
                    if (RecB[a + 1, b].Tag.Equals(ss[0]))
                    {
                        return true;
                    }             
                }

                //v\\
                if (a - 1 >= 0)
                {                  
                    if (RecB[a - 1, b].Tag.Equals(ss[0]))
                    {
                        return true;
                    }                 
                }
            }

            return false;
        }

        /// <summary>
        /// Проверяет соседние (левые - правые) на наличие своей территории (т.е того цвета который сейчас ходит)
        /// </summary>   
        /// <param name="sen">Список выделенных прямоугольников</param>
        /// <returns>Можно ли ставить блок или нет</returns>
       private bool LeftRightCheck(List<Rectangle> sen)
        {

            string[] ss = TurnL.Text.Split(' ');

            for (int i = 0; i < sen.Count; i++)
            {

                int a = Grid.GetRow(sen[i]);
                int b = Grid.GetColumn(sen[i]);

                //<\\
                if (b + 1 < Blocks)
                {              
                    if (RecB[a, b + 1].Tag.Equals(ss[0]))
                    {
                        return true;
                    }                  
                }

                //>\\
                if (b - 1 >= 0)
                {                
                    if (RecB[a, b - 1].Tag.Equals(ss[0]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Очистка выделенных блоков
        /// </summary>
        private void HoverNull()
        {
           foreach(var block in tryBlocks)
           {
               block.Fill = Brushes.LightGray;
           }
        }

        /// <summary>
        /// Смена хода
        /// </summary>
        /// <param name="Turn">Номер игрока который будет ходить</param>
        /// <returns></returns>
        private String contentLab(int Turn)
        {
            if (Turn > maxValPlayers)
            {
                swapTurn = 1;
                Turn = 1;
            }

         //   if (Turn == colorPlayer + 1)
           //     this.IsEnabled = true;
         //   else
         //       this.IsEnabled = false;

            switch(Turn)
            {
                case 1:
                    TurnL.Background = Brushes.DarkBlue;
                    return "Blue Turn";
                case 2:
                    TurnL.Background = Brushes.DarkRed;
                    return "Red Turn";
                case 3:
                    TurnL.Background = Brushes.Yellow;
                    return "Yellow Turn";
                case 4:
                    TurnL.Background = Brushes.Green;
                    return "Green Blue";
                default :
                    return "Error";
            }
        }
       
        /// <summary>
        /// Создает в выбранной области блок
        /// </summary>
        /// <param name="sender">Rectangle</param>
        /// <param name="e">MouseLeftDown</param>
        private void CreateTerritory(object sender, MouseButtonEventArgs e)
        {
          //string coor = (sender as Rectangle).Tag.ToString();
            //SendCoorToService(coor, Kost1, Kost2);
            CreateNewBloock(Kost1,Kost2);
        }
        
        /// <summary>
        /// Создание нового блока на игровом поле
        /// </summary>
        /// <param name="Kost1">Размер кости 1</param>
        /// <param name="Kost2">Размер кости 2</param>
        private void CreateNewBloock(double Kost1, double Kost2)
        {
            try
            {
                string coor = string.Empty;
                string[] ss = TurnL.Text.Split(' ');
                int positionBlockRow = -1;
                int positionBlockCol = -1;
                for (int i = 0; i < Blocks; i++)
                {
                    for (int j = 0; j < Blocks; j++)
                    {
                        if (RecB[i, j].Fill.Equals(Brushes.Blue))
                        {
                            if (positionBlockCol == -1)
                                coor = RecB[i, j].Tag.ToString();

                            positionBlockRow = (positionBlockRow == -1) ? Grid.GetRow(RecB[i, j]) : positionBlockRow;
                            positionBlockCol = (positionBlockCol == -1) ? Grid.GetColumn(RecB[i, j]) : positionBlockCol;

                            GridGame.Children.Remove(RecB[i, j]);

                            RecB[i, j].Tag = ss[0];
                        }
                    }
                }

                

                Label setBlock = new Label
                {
                    Background = TurnL.Background
                };

                Grid.SetRow(setBlock, positionBlockRow);
                Grid.SetColumn(setBlock, positionBlockCol);

                Grid.SetRowSpan(setBlock, (int)Kost1);
                Grid.SetColumnSpan(setBlock, (int)Kost2);

                GridGame.Children.Add(setBlock);

                ToUpdateScore(Kost1 * Kost2);
                NewTurn();          
                SendCoorToService(coor, (int)Kost1, (int)Kost2);               
            }
            catch { }
        }

        /// <summary>
        /// Отправка на сервер данные о игре
        /// </summary>
        /// <param name="coor">Rectangle на который кликнул юзер</param>
        /// <param name="Kost1">кость 1</param>
        /// <param name="Kost2">кость 2</param>
        private void SendCoorToService(string coor, double Kost1, double Kost2)
        {
            MemoryStream ms = new MemoryStream(new byte[256], 0, 256, true, true);
            BinaryWriter writer = new BinaryWriter(ms);       

            writer.Write(5);
            writer.Write(coor);
            writer.Write(Kost1);
            writer.Write(Kost2);
            writer.Write(idGame);

            soket.Send(ms.GetBuffer());
        }

        /// <summary>
        /// Обновляет количество очков игрока
        /// </summary>
        /// <param name="p">Очки</param>
        private void ToUpdateScore(double p)
        {
            int score = Int32.Parse(scoresPlayers[swapTurn - 1].Content.ToString());
            scoresPlayers[swapTurn - 1].Content = score + p;
        }

        /// <summary>
        /// Смена хода
        /// </summary>
        private void NewTurn()
        {
            swapTurn++;
            TurnL.Text = contentLab(swapTurn);
            RndKost();
        }
        
        /// <summary>
        /// Рандом кубиков
        /// </summary>
        private void RndKost()
        {
            
            Kost1 = rnd.Next(1, 7);
            Kost2 = rnd.Next(1, 7);
            if (this.IsEnabled)
            {
                MessageBox.Show("Кость 1 : " + Kost1 + "\nКость 2 : " + Kost2);
            }
        }

        /// <summary>
        /// Заполнение поля
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        private Rectangle createRec(int i, int j)
        {
            Rectangle rec = new Rectangle();
          
            rec.Fill = Brushes.LightGray;
            

            rec.Stroke = Brushes.Black;

            rec.StrokeThickness = 0.25;
            Grid.SetRow(rec, i);
            Grid.SetColumn(rec, j);

            return rec;
        }

        /// <summary>
        /// Поворот прямоугольника
        /// </summary>
        /// <param name="sender">Rectangle</param>
        /// <param name="e">MouseRightDown</param>
        private void SwapKost(object sender, MouseButtonEventArgs e)
        {
            double Kost = Kost1;
            Kost1 = Kost2;
            Kost2 = Kost;

            Hovers(rememberHoverRec);
        }

        /// <summary>
        /// Пропуск хода!
        /// </summary>
        /// <param name="sender">кнопка</param>
        /// <param name="e">клик</param>
        private void Skips(object sender, RoutedEventArgs e)
        {
            NewTurn();

           // MemoryStream ms = new MemoryStream(new byte[32], 0, 32, true, true);
          //  BinaryWriter writer = new BinaryWriter(ms);
         //   writer.Write(6);
          //  writer.Write(idGame);
          //  soket.Send(ms.GetBuffer());
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">Closing</param>
        private void ExitApp(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (thread != null)
                thread.Abort();

            randWind.Close();
        }
    }
}
