using System.Text.Json;

namespace TechMove.GLMS.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CurrencyService> _logger;

        private decimal? _cachedRate;
        private DateTime _lastFetchTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(60);
        private const decimal DefaultRate = 18.50m;

        public CurrencyService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CurrencyService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<decimal> GetUsdToZarRateAsync()
        {
            if (_cachedRate.HasValue && DateTime.UtcNow - _lastFetchTime < _cacheDuration)
                return _cachedRate.Value;

            try
            {
                var baseUrl = _configuration["CurrencyApi:BaseUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _logger.LogWarning("CurrencyApi:BaseUrl is not configured. Using default rate.");
                    return DefaultRate;
                }

                var response = await _httpClient.GetStringAsync($"{baseUrl}USD");
                using var json = JsonDocument.Parse(response);

                if (json.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty("ZAR", out var zarRate) &&
                    zarRate.TryGetDecimal(out var rate))
                {
                    _cachedRate = rate;
                    _lastFetchTime = DateTime.UtcNow;
                    _logger.LogInformation("Exchange rate updated: 1 USD = {Rate} ZAR", rate);
                    return rate;
                }

                _logger.LogWarning("ZAR rate not found in API response. Using default rate.");
                return DefaultRate;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching exchange rate. Using default rate.");
                return DefaultRate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching exchange rate. Using default rate.");
                return DefaultRate;
            }
        }
    }
}