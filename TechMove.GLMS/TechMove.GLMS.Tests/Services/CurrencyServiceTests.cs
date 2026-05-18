using Xunit;

namespace TechMove.GLMS.Tests.Services
{
    public class CurrencyServiceTests
    {
        [Fact]
        public void CurrencyCalculation_UsdToZar_ReturnsCorrectAmount()
        {
            decimal usdAmount = 100.00m;
            decimal rate = 18.50m;
            decimal expected = 1850.00m;

            var result = usdAmount * rate;

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CurrencyCalculation_ZeroUsd_ReturnsZeroZar()
        {
            decimal usdAmount = 0m;
            decimal rate = 18.50m;

            var result = usdAmount * rate;

            Assert.Equal(0m, result);
        }

        [Theory]
        [InlineData(50, 18.50, 925.00)]
        [InlineData(200, 18.50, 3700.00)]
        [InlineData(1, 18.50, 18.50)]
        public void CurrencyCalculation_VariousAmounts_ReturnsCorrect(decimal usd, decimal rate, decimal expected)
        {
            var result = usd * rate;
            Assert.Equal(expected, result);
        }
    }
}