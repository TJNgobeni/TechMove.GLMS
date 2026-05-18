using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading.Tasks;
using Xunit;
using TechMove.GLMS.Controllers;
using TechMove.GLMS.Data;
using TechMove.GLMS.Models;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Services;
using Microsoft.EntityFrameworkCore;

namespace TechMove.GLMS.Tests.Controllers
{
    public class ServiceRequestsControllerTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new AppDbContext(options);
        }

        private static ServiceRequestsController CreateController(
            AppDbContext context,
            ICurrencyService currencyService,
            IValidationService validationService)
        {
            return new ServiceRequestsController(
                context,
                currencyService,
                validationService,
                new NullLogger<ServiceRequestsController>());
        }

        [Fact]
        public async Task CreateRequest_WhenContractExpired_ReturnsError()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Expired");

            var contract = new Contract
            {
                Id = 1,
                Status = ContractStatus.Expired,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(-5),
                ServiceLevel = "Standard"
            };
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();

            var currencyMock = new Mock<ICurrencyService>();
            currencyMock.Setup(x => x.GetUsdToZarRateAsync()).ReturnsAsync(18.50m);

            var validationMock = new Mock<IValidationService>();
            validationMock.Setup(x => x.ValidateContractForServiceRequestAsync(1))
                .ReturnsAsync((false, "Cannot create a service request. The parent contract is Expired or On Hold."));

            var controller = CreateController(context, currencyMock.Object, validationMock.Object);

            var request = new ServiceRequest
            {
                ContractId = 1,
                Description = "Test Request",
                Cost = 100,
                Status = "Pending"
            };

            // Act
            var result = await controller.Create(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.ViewData.ModelState.ContainsKey(""));
            var error = result.ViewData.ModelState[""]!.Errors[0].ErrorMessage;
            Assert.Contains("Expired", error);
        }

        [Fact]
        public async Task CreateRequest_WhenContractOnHold_ReturnsError()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_OnHold");

            var contract = new Contract
            {
                Id = 2,
                Status = ContractStatus.OnHold,
                StartDate = DateTime.Now.AddDays(-10),
                EndDate = DateTime.Now.AddDays(5),
                ServiceLevel = "Premium"
            };
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();

            var currencyMock = new Mock<ICurrencyService>();
            currencyMock.Setup(x => x.GetUsdToZarRateAsync()).ReturnsAsync(18.50m);

            var validationMock = new Mock<IValidationService>();
            validationMock.Setup(x => x.ValidateContractForServiceRequestAsync(2))
                .ReturnsAsync((false, "Cannot create a service request. The parent contract is Expired or On Hold."));

            var controller = CreateController(context, currencyMock.Object, validationMock.Object);

            var request = new ServiceRequest
            {
                ContractId = 2,
                Description = "Test Request",
                Cost = 100,
                Status = "Pending"
            };

            // Act
            var result = await controller.Create(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.ViewData.ModelState.ContainsKey(""));
            var error = result.ViewData.ModelState[""]!.Errors[0].ErrorMessage;
            Assert.Contains("On Hold", error);
        }

        [Fact]
        public async Task CreateRequest_WhenContractActive_RedirectsToIndex()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Active");

            var client = new Client { Id = 1, Name = "Test Client" };
            var contract = new Contract
            {
                Id = 3,
                ClientId = 1,
                Status = ContractStatus.Active,
                StartDate = DateTime.Now.AddDays(-5),
                EndDate = DateTime.Now.AddDays(10),
                ServiceLevel = "Standard"
            };
            context.Clients.Add(client);
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();

            var currencyMock = new Mock<ICurrencyService>();
            currencyMock.Setup(x => x.GetUsdToZarRateAsync()).ReturnsAsync(18.50m);

            var validationMock = new Mock<IValidationService>();
            validationMock.Setup(x => x.ValidateContractForServiceRequestAsync(3))
                .ReturnsAsync((true, "Valid"));

            var controller = CreateController(context, currencyMock.Object, validationMock.Object);

            var request = new ServiceRequest
            {
                ContractId = 3,
                Description = "Test Request",
                Cost = 100,
                Status = "Pending"
            };

            // Act
            var result = await controller.Create(request) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result!.ActionName);
        }

        [Fact]
        public async Task CreateRequest_CurrencyConversion_IsApplied()
        {
            // Arrange
            using var context = CreateInMemoryContext("TestDb_Currency");

            var client = new Client { Id = 1, Name = "Test Client" };
            var contract = new Contract
            {
                Id = 4,
                ClientId = 1,
                Status = ContractStatus.Active,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(30),
                ServiceLevel = "Standard"
            };
            context.Clients.Add(client);
            context.Contracts.Add(contract);
            await context.SaveChangesAsync();

            var currencyMock = new Mock<ICurrencyService>();
            currencyMock.Setup(x => x.GetUsdToZarRateAsync()).ReturnsAsync(18.50m);

            var validationMock = new Mock<IValidationService>();
            validationMock.Setup(x => x.ValidateContractForServiceRequestAsync(4))
                .ReturnsAsync((true, "Valid"));

            var controller = CreateController(context, currencyMock.Object, validationMock.Object);

            var request = new ServiceRequest
            {
                ContractId = 4,
                Description = "Currency Test",
                Cost = 100,
                Status = "Pending"
            };

            // Act
            await controller.Create(request);

            // Assert
            var saved = await context.ServiceRequests.FirstOrDefaultAsync(s => s.ContractId == 4);
            Assert.NotNull(saved);
            Assert.Equal(1850.00m, saved!.CostZAR);
        }
    }
}