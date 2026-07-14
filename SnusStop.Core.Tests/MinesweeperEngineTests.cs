using SnusStop.Core.Games;

namespace SnusStop.Core.Tests;

public class MinesweeperEngineTests
{
    private static int CountMines(MinesweeperEngine e)
    {
        int n = 0;
        foreach (var cell in e.Grid) if (cell.IsMine) n++;
        return n;
    }

    [Fact]
    public void Board_has_the_requested_mine_count_after_first_click()
    {
        var e = new MinesweeperEngine(8, 10, seed: 42);
        e.Reveal(0, 0);
        Assert.Equal(10, CountMines(e));
    }

    [Fact]
    public void First_click_is_always_safe()
    {
        var e = new MinesweeperEngine(8, 10, seed: 42);
        e.Reveal(3, 3);
        Assert.False(e.Grid[3, 3].IsMine);
        Assert.False(e.Lost);
        Assert.Equal(CellState.Revealed, e.Grid[3, 3].State);
    }

    [Fact]
    public void Flagging_toggles_and_adjusts_mines_left()
    {
        var e = new MinesweeperEngine(8, 10, seed: 1);
        Assert.Equal(10, e.MinesLeft);
        e.ToggleFlag(0, 0);
        Assert.Equal(9, e.MinesLeft);
        Assert.Equal(CellState.Flagged, e.Grid[0, 0].State);
        e.ToggleFlag(0, 0);
        Assert.Equal(10, e.MinesLeft);
    }

    [Fact]
    public void Revealing_a_mine_loses()
    {
        var e = new MinesweeperEngine(8, 10, seed: 3);
        e.Reveal(0, 0); // safe, places mines
        // Find a mine and reveal it.
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (e.Grid[r, c].IsMine) { e.Reveal(r, c); goto done; }
        done:
        Assert.True(e.Lost);
        Assert.False(e.Won);
    }

    [Fact]
    public void Revealing_all_safe_cells_wins()
    {
        var e = new MinesweeperEngine(4, 2, seed: 7);
        e.Reveal(0, 0);
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                if (!e.Grid[r, c].IsMine) e.Reveal(r, c);
        Assert.True(e.Won);
        Assert.False(e.Lost);
    }
}
