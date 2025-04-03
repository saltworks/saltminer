namespace Saltworks.SaltMiner.Ui.Api.Models
{
    public class CustomLogger<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public CustomLogger(ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            var name = typeof(T).FullName.Replace("Saltworks.SaltMiner.", "");
            _logger = factory.CreateLogger(name);
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
