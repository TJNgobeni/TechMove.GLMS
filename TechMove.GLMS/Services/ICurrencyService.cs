namespace TechMove.GLMS.Services
{
    public interface ICurrencyService
    {
        Task<decimal> GetUsdToZarRateAsync();
    }
}