namespace CloodKey.Interfaces
{
    public abstract class KeyCli
    {
        public abstract string Get(string key);
        public abstract string Set(string key, string value);
    }
}