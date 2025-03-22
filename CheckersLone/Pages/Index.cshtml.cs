using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CheckersLone.Pages
{
    public class IndexModel : PageModel
    {
        public string[,] Board { get; private set; } = new string[8, 8];

        public void OnGet()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        if (row < 3)
                            Board[row, col] = "red"; // Pionki gracza 1
                        else if (row >= 5)
                            Board[row, col] = "blue"; // Pionki gracza 2
                        else
                            Board[row, col] = null; // Puste czarne pole
                    }
                    else
                    {
                        Board[row, col] = null; // Bia³e pole
                    }
                }
            }
        }
    }

}
