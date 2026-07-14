namespace SnusStop.Core.Games;

public class MemoryCard
{
    public int Index { get; init; }
    public int PairId { get; init; }
    public string Face { get; init; } = "";
    public bool Matched { get; set; }
    public bool FaceUp { get; set; }
}

/// <summary>
/// Concentration/memory-match. Flip two cards; a matching pair stays up, a mismatch is turned
/// back down on the next flip. Won when every pair is matched.
/// </summary>
public class MemoryMatchEngine
{
    /// <summary>
    /// One distinct face per pair. Needs at least as many entries as the largest board offers
    /// (18 pairs), since a repeated face would make a match ambiguous.
    /// </summary>
    private static readonly string[] Faces =
    {
        "🌱", "💚", "🏃", "💰", "🫁", "⭐", "🔥", "🎯",
        "🏆", "🎉", "🧘", "🍀", "💪", "🌈", "☀️", "🫀", "🥊", "🎁",
    };

    public IReadOnlyList<MemoryCard> Cards { get; }
    public bool Won { get; private set; }

    private readonly List<int> _up = new();

    public MemoryMatchEngine(int pairs = 8, int? seed = null)
    {
        pairs = Math.Clamp(pairs, 2, Faces.Length);
        var rng = seed is int s ? new Random(s) : new Random();

        var deck = new List<MemoryCard>();
        for (int p = 0; p < pairs; p++)
        {
            deck.Add(new MemoryCard { PairId = p, Face = Faces[p] });
            deck.Add(new MemoryCard { PairId = p, Face = Faces[p] });
        }
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        var cards = new List<MemoryCard>(deck.Count);
        for (int i = 0; i < deck.Count; i++)
            cards.Add(new MemoryCard { Index = i, PairId = deck[i].PairId, Face = deck[i].Face });
        Cards = cards;
    }

    /// <summary>Flips a card face up. Returns true when the flip completes a matching pair.</summary>
    public bool Flip(int index)
    {
        if (Won || index < 0 || index >= Cards.Count) return false;
        var card = Cards[index];
        if (card.Matched || card.FaceUp) return false;

        if (_up.Count == 2)
        {
            foreach (var u in _up) Cards[u].FaceUp = false;
            _up.Clear();
        }

        card.FaceUp = true;
        _up.Add(index);

        if (_up.Count == 2)
        {
            var a = Cards[_up[0]];
            var b = Cards[_up[1]];
            if (a.PairId == b.PairId)
            {
                a.Matched = true;
                b.Matched = true;
                _up.Clear();
                Won = Cards.All(c => c.Matched);
                return true;
            }
        }
        return false;
    }

    /// <summary>Indices currently awaiting a match/reset (0, 1, or 2).</summary>
    public IReadOnlyList<int> FaceUpIndices => _up;

    /// <summary>True when two non-matching cards are face up, awaiting a flip-back.</summary>
    public bool HasPendingMismatch => _up.Count == 2;

    /// <summary>Turns the current unmatched pair face down (used for the timed auto-hide).</summary>
    public void ResetUnmatched()
    {
        foreach (var i in _up) Cards[i].FaceUp = false;
        _up.Clear();
    }
}
