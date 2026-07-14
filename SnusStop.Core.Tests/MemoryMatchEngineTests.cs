using SnusStop.Core.Games;

namespace SnusStop.Core.Tests;

public class MemoryMatchEngineTests
{
    [Fact]
    public void Eight_pairs_make_sixteen_cards()
    {
        var e = new MemoryMatchEngine(8, seed: 5);
        Assert.Equal(16, e.Cards.Count);
        Assert.Equal(8, e.Cards.Select(c => c.PairId).Distinct().Count());
    }

    [Fact]
    public void Matching_flip_returns_true_and_marks_pair()
    {
        var e = new MemoryMatchEngine(3, seed: 5);
        var g = e.Cards.GroupBy(c => c.PairId).First().Select(c => c.Index).ToList();
        Assert.False(e.Flip(g[0]));
        Assert.True(e.Flip(g[1]));
        Assert.True(e.Cards[g[0]].Matched);
        Assert.True(e.Cards[g[1]].Matched);
    }

    [Fact]
    public void Mismatched_flip_returns_false()
    {
        var e = new MemoryMatchEngine(3, seed: 5);
        int a = e.Cards.First(c => c.PairId == 0).Index;
        int b = e.Cards.First(c => c.PairId == 1).Index;
        Assert.False(e.Flip(a));
        Assert.False(e.Flip(b));
        Assert.False(e.Cards[a].Matched);
    }

    [Fact]
    public void Matching_every_pair_wins()
    {
        var e = new MemoryMatchEngine(4, seed: 9);
        foreach (var grp in e.Cards.GroupBy(c => c.PairId))
        {
            var idx = grp.Select(c => c.Index).ToList();
            e.Flip(idx[0]);
            e.Flip(idx[1]);
        }
        Assert.True(e.Won);
    }
}
