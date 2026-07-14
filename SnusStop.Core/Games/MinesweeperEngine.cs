namespace SnusStop.Core.Games;

public enum CellState { Hidden, Revealed, Flagged }

public class Cell
{
    public bool IsMine { get; set; }
    public int Adjacent { get; set; }
    public CellState State { get; set; } = CellState.Hidden;
}

/// <summary>
/// Classic minesweeper. Mines are placed on the first reveal (first click is always safe),
/// zero-cells flood-reveal their neighbours.
/// </summary>
public class MinesweeperEngine
{
    public int Size { get; }
    public int Mines { get; }
    public Cell[,] Grid { get; }
    public bool Won { get; private set; }
    public bool Lost { get; private set; }

    private bool _placed;
    private readonly Random _rng;

    public MinesweeperEngine(int size = 8, int mines = 10, int? seed = null)
    {
        Size = size;
        Mines = Math.Min(mines, size * size - 1);
        _rng = seed is int s ? new Random(s) : new Random();
        Grid = new Cell[size, size];
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                Grid[r, c] = new Cell();
    }

    public int MinesLeft
    {
        get
        {
            int flags = 0;
            foreach (var cell in Grid)
                if (cell.State == CellState.Flagged) flags++;
            return Mines - flags;
        }
    }

    public bool GameOver => Won || Lost;

    private bool InBounds(int r, int c) => r >= 0 && r < Size && c >= 0 && c < Size;

    public void Reveal(int r, int c)
    {
        if (GameOver || !InBounds(r, c)) return;
        if (!_placed) { PlaceMines(r, c); _placed = true; }

        var cell = Grid[r, c];
        if (cell.State != CellState.Hidden) return;

        if (cell.IsMine)
        {
            cell.State = CellState.Revealed;
            Lost = true;
            RevealAllMines();
            return;
        }

        FloodReveal(r, c);
        CheckWin();
    }

    public void ToggleFlag(int r, int c)
    {
        if (GameOver || !InBounds(r, c)) return;
        var cell = Grid[r, c];
        if (cell.State == CellState.Revealed) return;
        cell.State = cell.State == CellState.Flagged ? CellState.Hidden : CellState.Flagged;
    }

    private void FloodReveal(int r, int c)
    {
        if (!InBounds(r, c)) return;
        var cell = Grid[r, c];
        if (cell.State != CellState.Hidden || cell.IsMine) return;

        cell.State = CellState.Revealed;
        if (cell.Adjacent != 0) return;

        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
                if (dr != 0 || dc != 0) FloodReveal(r + dr, c + dc);
    }

    private void PlaceMines(int safeR, int safeC)
    {
        var positions = new List<(int r, int c)>();
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                if (!(r == safeR && c == safeC)) positions.Add((r, c));

        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }

        for (int i = 0; i < Mines; i++)
            Grid[positions[i].r, positions[i].c].IsMine = true;

        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
            {
                if (Grid[r, c].IsMine) continue;
                int n = 0;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                        if (!(dr == 0 && dc == 0) && InBounds(r + dr, c + dc) && Grid[r + dr, c + dc].IsMine) n++;
                Grid[r, c].Adjacent = n;
            }
    }

    private void RevealAllMines()
    {
        foreach (var cell in Grid)
            if (cell.IsMine) cell.State = CellState.Revealed;
    }

    private void CheckWin()
    {
        foreach (var cell in Grid)
            if (!cell.IsMine && cell.State != CellState.Revealed) return;
        Won = true;
    }
}
