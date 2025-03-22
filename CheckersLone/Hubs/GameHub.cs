using CheckersLone.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using CheckersLone.Models;
using CheckersLone.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


namespace CheckersLone.Hubs
{
   

    public class GameHub : Hub
    {

        private static CheckersGame Game = new CheckersGame();


        public async Task JoinGame(string gameId, string playerName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

            // Wyślij obecny stan gry do nowego gracza
            await Clients.Caller.SendAsync("UpdateBoard", _gameService.Board);
            await Clients.Caller.SendAsync("UpdateCurrentPlayer", _gameService.CurrentPlayer);

            // Wyślij listę graczy i przypisanych kolorów
            await Clients.Caller.SendAsync("PlayerColors", PlayerColors);

            await Clients.Group(gameId).SendAsync("PlayerJoined", playerName);
        }
        private static Dictionary<string, string> PlayerColors = new Dictionary<string, string>();

        public async Task ChooseColor(string color)
        {
            string connectionId = Context.ConnectionId;

            if (PlayerColors.Values.Contains(color))
            {
                await Clients.Caller.SendAsync("ColorAlreadyTaken");
                return;
            }

            PlayerColors[connectionId] = color;
            await Clients.Caller.SendAsync("ColorAssigned", color);
        }




        private readonly GameService _gameService;
        private readonly IServiceProvider _serviceProvider;

        public GameHub(GameService gameService, IServiceProvider serviceProvider)
        {
            _gameService = gameService;
            _serviceProvider = serviceProvider;
        }
        private async Task NotifyGameOver(string winner)
        {
            await Clients.All.SendAsync("GameOver", winner);
        }

        private static Dictionary<string, string> LoggedInUsers = new Dictionary<string, string>();

        public async Task Login(string playerName, string playerPassword)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var player = await dbContext.Players.FirstOrDefaultAsync(p => p.Name == playerName);

            if (player != null && player.Password != playerPassword)
            {
                await Clients.Caller.SendAsync("LoginResult", new { success = false, reason = "InvalidPassword" });
                return;
            }

            if (player == null)
            {
                // Rejestracja nowego użytkownika
                player = new Player { Name = playerName, Password = playerPassword };
                dbContext.Players.Add(player);
                await dbContext.SaveChangesAsync();

                await Clients.Caller.SendAsync("LoginResult", new { success = true, isNewUser = true });
                return;
            }

            // Użytkownik istnieje i hasło jest poprawne
            await Clients.Caller.SendAsync("LoginResult", new { success = true, isNewUser = false });
            // Dodanie użytkownika do listy zalogowanych
            if (!LoggedInUsers.ContainsKey(Context.ConnectionId))
            {
                LoggedInUsers.Add(Context.ConnectionId, playerName);
            }

            // Powiadom wszystkich o zaktualizowanej liście
            await Clients.All.SendAsync("UpdateLoggedInUsers", LoggedInUsers.Values.ToList());
        }



        public async Task RestartGame()
        {
            _gameService.RestartGame();
            // Zresetuj kolory graczy, aby mogli ponownie wybrać
            PlayerColors.Clear();

            // Wyślij nową planszę do wszystkich graczy
            await Clients.All.SendAsync("UpdateBoard", _gameService.Board);

            // Wyślij informację o aktualnym graczu
            await Clients.All.SendAsync("UpdateCurrentPlayer", _gameService.CurrentPlayer);

            // Powiadom graczy o rozpoczęciu nowej gry (potrzebują ponownie wybrać kolory)
            await Clients.All.SendAsync("GameRestarted");

            // Jeśli chcesz, aby gracze mogli ponownie wybrać kolory, wyślij komunikat, że restart gry jest zakończony
            await Clients.All.SendAsync("ColorSelectionReset");

            await Clients.All.SendAsync("GameRestarted");
        }

        public async Task GetBoard()
        {
            await Clients.Caller.SendAsync("UpdateBoard", _gameService.Board);
        }

        public async Task MakeMove(int fromX, int fromY, int toX, int toY)
        {
            string connectionId = Context.ConnectionId;

            if (!PlayerColors.TryGetValue(connectionId, out string playerColor))
            {
                await Clients.Caller.SendAsync("NoColorSelected");
                return;
            }

            if (_gameService.Board[fromX][fromY] == null || !_gameService.Board[fromX][fromY].StartsWith(playerColor))
            {
                await Clients.Caller.SendAsync("InvalidMove");
                return;
            }

            if (_gameService.MakeMove(fromX, fromY, toX, toY))
            {
                await Clients.All.SendAsync("UpdateBoard", _gameService.Board);
                await Clients.All.SendAsync("UpdateCurrentPlayer", _gameService.CurrentPlayer);
            }
            else
            {
                await Clients.Caller.SendAsync("InvalidMove");
            }
        }

        public void EndGame(string winner)
        {
            // Zapisanie wyniku w bazie danych
            _gameService.EndGame(winner);
        }

        public async Task<List<List<int>>> GetValidMoves(int fromX, int fromY)
        {
            var validMoves = _gameService.GetValidMoves(fromX, fromY);

            // Zwrócenie listy krotek jako tablica
            return validMoves.Select(move => new List<int> { move.Item1, move.Item2 }).ToList();
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;

            // Sprawdź, czy gracz ma przypisany kolor
            if (PlayerColors.TryGetValue(connectionId, out string color))
            {
                Console.WriteLine($"Klient {Context.ConnectionId} został rozłączony. Powód: {exception?.Message}");
                // Usuń gracza z listy kolorów
                PlayerColors.Remove(connectionId);

                // Wyślij komunikat do pozostałych graczy o zwolnieniu koloru
                await Clients.All.SendAsync("ColorReleased", color);
            }

            await base.OnDisconnectedAsync(exception);
        }

       
        public async Task Ping()
        {
            Console.WriteLine($"Ping od klienta {Context.ConnectionId}");
            await Task.CompletedTask;
        }

        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

    }

}
