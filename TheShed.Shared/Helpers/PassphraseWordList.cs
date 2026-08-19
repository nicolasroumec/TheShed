namespace TheShed.Shared.Helpers
{
    // ponytail: hand-picked list of ~350 common, unambiguous English words instead of
    // vendoring the full EFF wordlist (7776 entries) as a file/embedded resource — simpler
    // to ship (no build config, no I/O) and gives ~8.5 bits/word, enough for a "memorable"
    // convenience mode. Swap for the real EFF list if higher per-word entropy is wanted.
    internal static class PassphraseWordList
    {
        public static readonly string[] Words =
        {
            "abandon", "ability", "abroad", "absent", "absorb", "accent", "accept", "access",
            "accord", "account", "accuse", "achieve", "acid", "acorn", "acre", "action",
            "active", "actor", "adapt", "add", "adjust", "admire", "admit", "adopt",
            "adult", "advance", "advice", "affair", "afford", "afraid", "again", "agency",
            "agent", "agree", "ahead", "aim", "air", "airport", "alarm", "album",
            "alert", "alien", "alike", "alive", "alley", "allow", "almost", "alone",
            "along", "alpha", "already", "also", "alter", "always", "amateur", "amazing",
            "among", "amount", "ample", "amuse", "analyst", "anchor", "ancient", "angle",
            "angry", "animal", "ankle", "annual", "answer", "antique", "anxiety", "apart",
            "apple", "apply", "april", "arch", "area", "arena", "argue", "arm",
            "armor", "army", "around", "arrange", "arrest", "arrive", "arrow", "art",
            "artist", "aspect", "asset", "assist", "assume", "athlete", "atom", "attach",
            "attack", "attend", "attic", "auction", "audit", "august", "author", "auto",
            "autumn", "avenue", "average", "avocado", "avoid", "awake", "aware", "away",
            "awesome", "axis", "baby", "bachelor", "badge", "bag", "balance", "balcony",
            "ball", "bamboo", "banana", "banner", "barely", "bargain", "barrel", "base",
            "basic", "basket", "battle", "beach", "bean", "bear", "beauty", "become",
            "beef", "before", "begin", "behave", "behind", "believe", "belong", "below",
            "belt", "bench", "benefit", "best", "betray", "better", "between", "beyond",
            "bicycle", "bike", "bind", "biology", "bird", "birth", "bitter", "black",
            "blade", "blame", "blanket", "blast", "bleak", "bless", "blind", "blood",
            "blossom", "blue", "blush", "board", "boat", "body", "boil", "bomb",
            "bonus", "book", "boost", "border", "boring", "borrow", "boss", "bottom",
            "bounce", "box", "boy", "brain", "branch", "brand", "brass", "brave",
            "bread", "break", "breeze", "brick", "bridge", "brief", "bright", "bring",
            "broad", "broken", "bronze", "broom", "brother", "brown", "brush", "bubble",
            "budget", "buffalo", "build", "bulb", "bulk", "bullet", "bundle", "bunker",
            "burden", "burger", "burst", "bus", "bush", "business", "butter", "buyer",
            "cabin", "cable", "cactus", "cage", "cake", "call", "calm", "camera",
            "camp", "canal", "cancel", "candy", "cannon", "canoe", "canvas", "canyon",
            "capable", "capital", "captain", "carbon", "card", "cargo", "carpet", "carry",
            "cart", "case", "cash", "castle", "casual", "catalog", "catch", "category",
            "cattle", "cause", "cave", "ceiling", "celery", "cement", "census", "century",
            "cereal", "certain", "chair", "chalk", "champion", "change", "chaos", "charge",
            "chase", "cheap", "check", "cheese", "chef", "cherry", "chest", "chicken",
            "chief", "child", "chimney", "choice", "choose", "chronic", "chuckle", "chunk",
            "circle", "citizen", "city", "civil", "claim", "clap", "clarify", "claw",
            "clay", "clean", "clerk", "clever", "click", "client", "cliff", "climb",
            "clinic", "clock", "close", "cloth", "cloud", "clown", "club", "clump",
            "cluster", "coach", "coast", "coconut", "code", "coffee", "coil", "coin",
            "collect", "color", "column", "combine", "comfort", "comic", "common", "compass",
            "concert", "conduct", "confirm", "congress", "connect", "consider", "control", "convince",
            "cook", "cool", "copper", "copy", "coral", "core", "corn", "correct",
            "costume", "cotton", "couch", "country", "couple", "course", "cousin", "cover",
        };
    }
}
