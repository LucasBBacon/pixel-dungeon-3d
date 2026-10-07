namespace PixelDungeon.Core.Actors.Mobs;

public static class Bestiary
{
    // java reflects over Class<? extends Mob>
    // every ported mob registers a factory
    // unported entries are filtered out of the roll
    // add line when whole and later
    private static readonly Dictionary<Type, Func<Mob>> Registry = new()
    {
        [typeof(Rat)] = () => new Rat(),
        [typeof(Albino)] = () => new Albino(),
        [typeof(Gnoll)] = () => new Gnoll(),
        [typeof(Crab)] = () => new Crab(),
        [typeof(Swarm)] = () => new Swarm(),
        // TODO: Mimic, Statue, Wraith, Piranha, Goo
        // TODO: Skeleton, Thief, Bandit, Shaman, Tengu
        // TODO: Bat, Brute, Shielded, Spinner, DM300
        // TODO: Elemental, Monk, Senior, Warlock, Golem, King
        // TODO: Succubus, Eye, Scorpio, Acidic, Yog
    };

    private static Mob Create(Type cl) =>
        Registry.TryGetValue(cl, out var factory) ? factory() : null;

    public static Mob Mob(int depth)
    {
        var cl = MobClass(depth);
        return cl == null ? null : Create(cl);
    }

    public static Mob Mutable(int depth)
    {
        var cl = MobClass(depth);
        if (cl == null)
        {
            return null;
        }

        if (Random.Int(30) == 0)
        {
            if (cl == typeof(Rat)) cl = typeof(Albino);
            // TODO: Thief -> Bandit, Brute -> Shielded, Monk -> Senior, Scorpio -> Acidic
        }

        return Create(cl);
    }

    private static Type MobClass(int depth) => Roll(TableFor(depth));

    private static (float Chance, string Cls)[] TableFor(int depth) =>
        depth switch
        {
            1 => new[] { (1f, "Rat") },
            2 => new[] { (1f, "Rat"), (1f, "Gnoll") },
            3 => new[] { (1f, "Rat"), (2f, "Gnoll"), (1f, "Crab"), (0.02f, "Swarm") },
            4 => new[]
            {
                (1f, "Rat"), (2f, "Gnoll"), (3f, "Crab"), (0.02f, "Swarm"), (0.01f, "Skeleton"), (0.01f, "Thief")
            },

            5 => new[] { (1f, "Goo") },

            6 => new[] { (4f, "Skeleton"), (2f, "Thief"), (1f, "Swarm"), (0.2f, "Shaman") },
            7 => new[] { (3f, "Skeleton"), (1f, "Shaman"), (1f, "Thief"), (1f, "Swarm") },
            8 => new[]
            {
                (3f, "Skeleton"), (2f, "Shaman"), (1f, "Gnoll"), (1f, "Thief"), (1f, "Swarm"), (0.02f, "Bat")
            },
            9 => new[]
            {
                (3f, "Skeleton"), (3f, "Shaman"), (1f, "Thief"), (1f, "Swarm"), (0.02f, "Bat"), (0.01f, "Brute")
            },

            10 => new[] { (1f, "Tengu") },

            11 => new[] { (1f, "Bat"), (0.2f, "Brute") },
            12 => new[] { (1f, "Bat"), (1f, "Brute"), (0.2f, "Spinner") },
            13 => new[] { (1f, "Bat"), (3f, "Brute"), (1f, "Shaman"), (1f, "Spinner"), (0.02f, "Elemental") },
            14 => new[]
            {
                (1f, "Bat"), (3f, "Brute"), (1f, "Shaman"), (4f, "Spinner"), (0.02f, "Elemental"), (0.01f, "Monk")
            },

            15 => new[] { (1f, "DM300") },

            16 => new[] { (1f, "Elemental"), (1f, "Warlock"), (0.2f, "Monk") },
            17 => new[] { (1f, "Elemental"), (1f, "Monk"), (1f, "Warlock") },
            18 => new[] { (1f, "Elemental"), (2f, "Monk"), (1f, "Golem"), (1f, "Warlock") },
            19 => new[] { (1f, "Elemental"), (2f, "Monk"), (3f, "Golem"), (1f, "Warlock"), (0.02f, "Succubus") },

            20 => new[] { (1f, "King") },

            22 => new[] { (1f, "Succubus"), (1f, "Eye") },
            23 => new[] { (1f, "Succubus"), (2f, "Eye"), (1f, "Scorpio") },
            24 => new[] { (1f, "Succubus"), (2f, "Eye"), (3f, "Scorpio") },

            25 => new[] { (1f, "Yog") },

            _ => new[] { (1f, "Eye") },
        };

    private static Type Roll((float Chance, string Cls)[] table)
    {
        var chances = new List<float>();
        var classes = new List<Type>();

        foreach (var (chance, name) in table)
        {
            var cl = Registry.Keys.FirstOrDefault(k => k.Name == name);
            if (cl != null)
            {
                chances.Add(chance);
                classes.Add(cl);
            }
        }

        return classes.Count == 0 ? null : classes[Random.Chances([.. chances])];
    }

    public static bool IsBoss(Char m)
    {
        // TODO: Goo, Tengu, DM300, King, Yog
        return false;
    }

    public static IEnumerable<string> TableNamesForTest()
    {
        for (var depth = 1; depth <= 26; depth++)
        {
            foreach (var name in TableFor(depth).Select(e => e.Cls))
            {
                yield return name;
            }
        }
    }

    public static bool IsRegisteredForTest(string name)
    {
        return Registry.Keys.Any(k => k.Name == name);
    }
}