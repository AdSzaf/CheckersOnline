namespace CheckersLone.Models
{
    public class Board
    {
        public int Size { get; set; } = 8; // Standardowa plansza 8x8
        public Piece[,] Pieces { get; set; }

        public Board()
        {
            Pieces = new Piece[Size, Size];
            // Inicjalizacja pionków
        }
    }
}
