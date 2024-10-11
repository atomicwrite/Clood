namespace CloodKey.Interfaces
{
    public abstract class KeyCli
    {
        public virtual Task<string> Get(string key)
        {
            return Task.FromResult<string>("");
        }
        public virtual Task Delete(string key)
        {
            return Task.CompletedTask;
        }
        public virtual Task Set(string key, string value)
        {
            return Task.CompletedTask;
        }
    }
}