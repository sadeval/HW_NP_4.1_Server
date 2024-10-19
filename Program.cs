using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TicTacToeServer
{
    public class Program
    {
        private static TcpListener server;

        static void Main()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 5500);
                server.Start();
                Console.WriteLine("Сервер запущен... Ждем подключения..");

                while (true)
                {
                    Console.WriteLine("Ожидаем подключения клиента 1...");
                    var client1 = server.AcceptTcpClient();
                    Console.WriteLine($"Клиент 1 подключен: {((IPEndPoint)client1.Client.RemoteEndPoint).Address}");

                    Console.WriteLine("Ожидаем подключения клиента 2...");
                    var client2 = server.AcceptTcpClient();
                    Console.WriteLine($"Клиент 2 подключен: {((IPEndPoint)client2.Client.RemoteEndPoint).Address}");

                    // Запуск игры в отдельном потоке
                    Game game = new Game(client1, client2);
                    Thread gameThread = new Thread(game.Start);
                    gameThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сервера: {ex.Message}");
            }
        }
    }

    public class Game
    {
        private TcpClient player1;
        private TcpClient player2;
        private char[,] board = new char[3, 3];
        private char currentPlayer;
        private NetworkStream stream1;
        private NetworkStream stream2;
        private string mode; 

        public Game(TcpClient player1, TcpClient player2)
        {
            this.player1 = player1;
            this.player2 = player2;
            stream1 = player1.GetStream();
            stream2 = player2.GetStream();
            currentPlayer = 'X'; 
        }

        public void Start()
        {
            InitializeBoard();
            ChooseGameMode();

            while (true)
            {
                PlayTurn(player1, stream1);
                if (CheckGameStatus())
                    break;

                if (mode == "Человек-человек")
                {
                    PlayTurn(player2, stream2); 
                }
                else if (mode == "Человек-компьютер")
                {
                    PlayComputerTurn(); 
                }
                else if (mode == "Компьютер-компьютер")
                {
                    PlayComputerTurn(); 
                    if (CheckGameStatus())
                        break;
                    PlayComputerTurn(); 
                }

                if (CheckGameStatus())
                    break;
            }
        }

        private void InitializeBoard()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    board[i, j] = ' ';
        }

        private void ChooseGameMode()
        {
            byte[] modeBuffer = new byte[20];
            stream1.Write(Encoding.UTF8.GetBytes("Выберите режим: "), 0, 49);
            stream1.Read(modeBuffer, 0, modeBuffer.Length);
            mode = Encoding.UTF8.GetString(modeBuffer).Trim();
        }

        private void PlayTurn(TcpClient player, NetworkStream stream)
        {
            SendBoard(stream);
            byte[] buffer = new byte[2];
            stream.Read(buffer, 0, buffer.Length);
            int x = buffer[0] - '0';
            int y = buffer[1] - '0';

            if (board[x, y] == ' ')
            {
                board[x, y] = currentPlayer;
                currentPlayer = (currentPlayer == 'X') ? 'O' : 'X'; 
            }
        }

        private void PlayComputerTurn()
        {
            Random random = new Random();
            int x, y;
            do
            {
                x = random.Next(0, 3);
                y = random.Next(0, 3);
            } while (board[x, y] != ' ');

            board[x, y] = currentPlayer;
            SendBoard(stream1); 
            SendBoard(stream2); 

            currentPlayer = (currentPlayer == 'X') ? 'O' : 'X'; 
        }

        private void SendBoard(NetworkStream stream)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                    sb.Append(board[i, j]);
                sb.AppendLine();
            }
            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(data, 0, data.Length);
        }

        private bool CheckGameStatus()
        {
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] != ' ' && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
                {
                    SendResult($"{board[i, 0]} выиграл!");
                    return true;
                }
                if (board[0, i] != ' ' && board[0, i] == board[1, i] && board[1, i] == board[2, i])
                {
                    SendResult($"{board[0, i]} выиграл!");
                    return true;
                }
            }

            if (board[0, 0] != ' ' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
            {
                SendResult($"{board[0, 0]} выиграл!");
                return true;
            }
            if (board[0, 2] != ' ' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
            {
                SendResult($"{board[0, 2]} выиграл!");
                return true;
            }

            // Проверка на ничью
            bool isDraw = true;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[i, j] == ' ')
                        isDraw = false;

            if (isDraw)
            {
                SendResult("Ничья!");
                return true;
            }

            return false; 
        }

        private void SendResult(string result)
        {
            byte[] resultData1 = Encoding.UTF8.GetBytes(result);
            stream1.Write(resultData1, 0, resultData1.Length);
            stream2.Write(resultData1, 0, resultData1.Length);
        }


    }
}
