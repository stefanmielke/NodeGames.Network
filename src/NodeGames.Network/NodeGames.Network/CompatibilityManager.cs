namespace NodeGames.Network
{
    internal static class CompatibilityManager
    {
        public static int GetHashCode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return 0;

            unchecked
            {
                int hash = 23;
                foreach (char c in name)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
