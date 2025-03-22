using CheckersLone.Data;
using CheckersLone.Hubs;
using CheckersLone.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CheckersLone.Services
{
    public class GameService
    {

        public List<List<string>> Board { get; private set; }
        public string CurrentPlayer { get; private set; } = "red"; // Zaczyna gracz czerwony

        private bool continueCapture = false;

        private int captureX, captureY; // Pozycja pionka kontynuującego bicie

        private readonly IHubContext<GameHub> _hubContext;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GameService(IHubContext<GameHub> hubContext, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _hubContext = hubContext;
            InitializeBoard();
        }



        public void InitializeBoard()
        {
            Board = new List<List<string>>();
            for (int row = 0; row < 8; row++)
            {
                var rowList = new List<string>();
                for (int col = 0; col < 8; col++)
                {
                    if (row < 3 && (row + col) % 2 == 1)
                        rowList.Add("red");
                    else if (row > 4 && (row + col) % 2 == 1)
                        rowList.Add("blue");
                    else
                        rowList.Add(null);
                }
                Board.Add(rowList);
            }
        }



        public bool MakeMove(int fromX, int fromY, int toX, int toY)
        {
            if (continueCapture && (fromX != captureX || fromY != captureY)) 
                return false; // Tylko kontynuujący pionek może wykonać ruch

            string currentPlayer = Board[fromX][fromY];

            if (currentPlayer == null || Board[toX][toY] != null || !currentPlayer.StartsWith(CurrentPlayer))
                return false;


            int dx = toX - fromX;
            int dy = toY - fromY;
            // Logika ruchu damki z bicie
            if (currentPlayer.EndsWith("_king"))
            {
                if (Math.Abs(dx) == Math.Abs(dy)) // Ruch ukośny
                {
                    int stepX = dx > 0 ? 1 : -1;
                    int stepY = dy > 0 ? 1 : -1;

                    bool foundEnemy = false; // Czy na trasie jest pionek przeciwnika
                    int x = fromX + stepX, y = fromY + stepY;

                    int enemyX = -1, enemyY = -1; // Pozycja pionka przeciwnika

                    while (x != toX && y != toY)
                    {
                        if (Board[x][y] != null)
                        {
                            if (Board[x][y].StartsWith(CurrentPlayer)) // Własny pionek blokuje ruch
                                return false;

                            if (foundEnemy) // Więcej niż jeden przeciwnik na trasie
                                return false;

                            foundEnemy = true;
                            enemyX = x; 
                            enemyY = y;
                        }

                        x += stepX;
                        y += stepY;
                    }

                    if (foundEnemy) // Jeśli bicie
                    {
                        Board[enemyX][enemyY] = null; // Usuń zbity pionek
                    }

                    Board[toX][toY] = currentPlayer;
                    Board[fromX][fromY] = null;

                    ChangeTurn();
                    if (IsGameOver())
                    {
                        return true; // Jeśli gra się kończy, zwracamy sukces, ale dalsza logika nie ma znaczenia
                    }
                    return true;
                }
            }

            // Obsługa ruchu o jedno pole
            if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1 && !continueCapture)
            {
                // Zwykły ruch (dla pionków)
                if ((currentPlayer == "red" && dx == 1) || (currentPlayer == "blue" && dx == -1))
                {
                    Board[toX][toY] = currentPlayer;
                    Board[fromX][fromY] = null;
                    CheckForPromotion(toX, toY);
                    ChangeTurn();
                    if (IsGameOver())
                    {
                        return true; // Jeśli gra się kończy, zwracamy sukces, ale dalsza logika nie ma znaczenia
                    }
                    return true;
                }
            }

            // Obsługa bicia
            if (Math.Abs(dx) == 2 && Math.Abs(dy) == 2)
            {
                int middleX = fromX + dx / 2;
                int middleY = fromY + dy / 2;

                if (Board[middleX][middleY] != null && Board[middleX][middleY] != currentPlayer)
                {
                    Board[toX][toY] = currentPlayer;
                    Board[fromX][fromY] = null;
                    Board[middleX][middleY] = null; // Usuwamy zbity pionek
                    CheckForPromotion(toX, toY);
                    if (CanCapture(toX, toY))
                    {
                        continueCapture = true; // Oczekuj na kolejny ruch bicia return true;
                        captureX = toX; 
                        captureY = toY;
                        return true;
                    }

                    ChangeTurn();
                    if (IsGameOver())
                    {
                        return true; // Jeśli gra się kończy, zwracamy sukces, ale dalsza logika nie ma znaczenia
                    }
                    continueCapture = false;
                    return true;
                }
            }

            return false;
        }
        private void CheckForPromotion(int x, int y)
        {
            if (Board[x][y] == "red" && x == 7)
                Board[x][y] = "red_king";
            if (Board[x][y] == "blue" && x == 0)
                Board[x][y] = "blue_king";
        }
        private void ChangeTurn()
        {
            CurrentPlayer = CurrentPlayer == "red" ? "blue" : "red";
            continueCapture = false; // Resetujemy flagę po zmianie tury
        }

        private bool CanCapture(int fromX, int fromY) 
        { int[] dx = { 1, 1, -1, -1 };
          int[] dy = { 1, -1, 1, -1 };
          string currentPlayer = Board[fromX][fromY];
          for (int dir = 0; dir < 4; dir++) 
          { 
                int x = fromX + dx[dir];
                int y = fromY + dy[dir];
                if (x + dx[dir] >= 0 && y + dy[dir] >= 0 && x + dx[dir] < 8 && y + dy[dir] < 8)
                {
                    if (Board[x][y] != null && !Board[x][y].StartsWith(currentPlayer) && Board[x + dx[dir]][y + dy[dir]] == null) 
                        return true;
                }
            } 
            return false;
        }

        public List<(int, int)> GetValidMoves(int fromX, int fromY)
        {
            var moves = new List<(int, int)>();
            string currentPlayer = Board[fromX][fromY];

            if (currentPlayer == null)
                return moves;

            // Kierunki ruchu dla normalnych pionków
            int[] dx = { 1, 1, -1, -1 };
            int[] dy = { 1, -1, 1, -1 };

            // Sprawdzamy ruchy dla pionka
            for (int i = 0; i < 4; i++)
            {
                int toX = fromX + dx[i];
                int toY = fromY + dy[i];

                // Sprawdzenie możliwego ruchu o jedno pole
                if (toX >= 0 && toY >= 0 && toX < 8 && toY < 8 && Board[toX][toY] == null)
                {
                    if ((currentPlayer == "red" && dx[i] == 1) || (currentPlayer == "blue" && dx[i] == -1) || currentPlayer.EndsWith("_king"))
                        moves.Add((toX, toY));
                }

                // Sprawdzenie możliwego bicia
                toX = fromX + 2 * dx[i];
                toY = fromY + 2 * dy[i];
                int middleX = fromX + dx[i];
                int middleY = fromY + dy[i];

                if (toX >= 0 && toY >= 0 && toX < 8 && toY < 8 && Board[toX][toY] == null && Board[middleX][middleY] != null && !Board[middleX][middleY].StartsWith(currentPlayer))
                {
                    moves.Add((toX, toY));
                }
            }

            // Jeśli gracz ma damkę, sprawdzamy ruchy w kierunkach 4 przekątnych przez dowolną liczbę pól
            if (currentPlayer.EndsWith("_king"))
            {
                // Sprawdzanie dla każdej z 4 przekątnych (wiele pól)
                for (int i = 0; i < 4; i++)
                {
                    int step = 1;
                    while (true)
                    {
                        int toX = fromX + dx[i] * step;
                        int toY = fromY + dy[i] * step;

                        // Jeśli wyjdzie poza planszę, zatrzymujemy sprawdzanie w tym kierunku
                        if (toX < 0 || toY < 0 || toX >= 8 || toY >= 8)
                            break;

                        // Jeśli pole jest puste, można się tam ruszyć
                        if (Board[toX][toY] == null)
                        {
                            moves.Add((toX, toY));
                        }
                        // Jeśli pole jest zajęte przez przeciwnika, sprawdzamy możliwość bicia
                        else if (Board[toX][toY] != null && !Board[toX][toY].StartsWith(currentPlayer))
                        {
                            // Sprawdzamy pole za tymi pionkami (czy jest puste)
                            int afterX = toX + dx[i];
                            int afterY = toY + dy[i];
                            if (afterX >= 0 && afterY >= 0 && afterX < 8 && afterY < 8 && Board[afterX][afterY] == null)
                            {
                                moves.Add((afterX, afterY));
                            }
                            break; // Po bicie przerywamy szukanie w tym kierunku
                        }
                        // Jeśli pole jest zajęte przez swojego pionka, zatrzymujemy sprawdzanie w tym kierunku
                        else
                        {
                            break;
                        }
                        step++;
                    }
                }
            }

            return moves;
        }



        public bool IsGameOver()
        {
            // 1. Sprawdzenie, czy przeciwnik ma jakiekolwiek pionki
            bool hasRedPieces = Board.Any(row => row.Any(cell => cell?.StartsWith("red") == true));
            bool hasBluePieces = Board.Any(row => row.Any(cell => cell?.StartsWith("blue") == true));

            if (!hasRedPieces)
            {
                EndGame("blue");
                return true;
            }

            if (!hasBluePieces)
            {
                EndGame("red");
                return true;
            }

            // 2. Sprawdzenie, czy przeciwnik ma możliwe ruchy
            bool opponentCanMove = false;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (Board[row][col]?.StartsWith(CurrentPlayer == "red" ? "blue" : "red") == true)
                    {
                        var validMoves = GetValidMoves(row, col);
                        if (validMoves.Count > 0)
                        {
                            opponentCanMove = true;
                            break;
                        }
                    }
                }
                if (opponentCanMove)
                    break;
            }

            if (!opponentCanMove)
            {
                EndGame(CurrentPlayer); // Obecny gracz wygrywa, bo przeciwnik nie ma ruchów
                return true;
            }

            return false;
        }

        // Zmieniamy poziom dostępu do EndGame na public lub internal
        public void EndGame(string winner)
        {
            Console.WriteLine($"Gra zakończona! Zwycięzca: {winner}");

            // Zapisz wynik do bazy danych
            UpdateGameStats(winner);

            _hubContext.Clients.All.SendAsync("GameOver", winner);
        }

        private void UpdateGameStats(string winnerColor)
        {
            // Tworzymy instancję kontekstu
            using var context = _dbContextFactory.CreateDbContext();

            // Tworzymy nowy rekord w tabeli GameStats dla zwycięzcy
            var gameStat = new GameStats
            {
                Color = winnerColor,
                Wins = 1,  // Każdy rekord oznacza jedną wygraną
                Date = DateTime.Now  // Przypisujemy prawdziwą datę i godzinę
            };

            // Dodajemy nowy rekord do tabeli
            context.GameStats.Add(gameStat);

            // Zapisz zmiany w bazie danych
            context.SaveChanges();
        }


        public void RestartGame()
        {
            InitializeBoard(); // Resetowanie planszy
            CurrentPlayer = "red"; // Ustawienie początkowego gracza
            continueCapture = false; // Reset flagi kontynuacji bicia
            Console.WriteLine("Gra została zrestartowana.");
            _hubContext.Clients.All.SendAsync("GameRestarted", Board, CurrentPlayer);
        }

        public async Task<object> Login(string playerName, string playerPassword)
        {
            // Tu możesz podłączyć logikę bazy danych
            using var dbContext = _dbContextFactory.CreateDbContext();
            var player = dbContext.Players.SingleOrDefault(p => p.Name == playerName);

            if (player != null && player.Password == playerPassword)
            {
                // Zalogowano pomyślnie
                return new { success = true };
            }

            // Nieprawidłowy Nick lub hasło
            return new { success = false };
        }

        public List<Player> GetAllPlayers()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Players.ToList();
        }



    }


}
