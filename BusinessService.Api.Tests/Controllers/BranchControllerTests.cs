
using BusinessService.Api.Controllers;
using BusinessService.Application.DTOs;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BusinessService.Api.Tests.Controllers;

[TestFixture]
public class BranchControllerTests
{
    private Mock<IBusinessService> _serviceMock = null!;
    private Mock<ILogger<BusinessBranchesController>> _loggerMock = null!;
    private BusinessBranchesController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _serviceMock = new Mock<IBusinessService>();
        _loggerMock = new Mock<ILogger<BusinessBranchesController>>();
        _controller = new BusinessBranchesController(
            _serviceMock.Object,
            _loggerMock.Object
        );
    }
    
    [Test]
    public async Task CreateBranch_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var dto = new BranchDto(
            BusinessId: Guid.NewGuid(),
            BranchName: "Main Branch",
            BranchStreet: "123 Street",
            BranchCityTown: "City",
            BranchState: "State"
        );
        
        var createdBranch = new BusinessBranches
        {
            Id = Guid.NewGuid(),
            BusinessId = dto.BusinessId,
            BranchName = dto.BranchName,
            BranchStreet = dto.BranchStreet,
            BranchCityTown = dto.BranchCityTown,
            BranchState = dto.BranchState,
            BranchStatus = "active"
        };


        _serviceMock
            .Setup(s => s.AddBranchesAsync(dto))
            .ReturnsAsync(createdBranch);

        // Act
        var result = await _controller.CreateBranch(dto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task CreateBranch_ReturnsNotFound_WhenBusinessNotFound()
    {
        // Arrange
        var dto = new BranchDto(
            BusinessId: Guid.NewGuid(),
            BranchName: "Main Branch",
            BranchStreet: "123 Street",
            BranchCityTown: "City",
            BranchState: "State"
        );


        _serviceMock
            .Setup(s => s.AddBranchesAsync(dto))
            .ThrowsAsync(new BusinessNotFoundException("Business not found"));

        // Act
        var result = await _controller.CreateBranch(dto);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }
    
    [Test]
    public async Task GetBusinessBranches_ReturnsOk_WithResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var branches = new List<BusinessBranches>
        {
            new BusinessBranches{Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), BranchName = "Branch Name", BranchStreet = null, BranchCityTown = null, BranchState = null, BranchStatus = "active"},
            new BusinessBranches{Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), BranchName = "Branch Name Two", BranchStreet = null, BranchCityTown = null, BranchState = null, BranchStatus = "active"},
        };

        _serviceMock
            .Setup(s => s.GetBusinessBranchesAsync(id))
            .ReturnsAsync(branches);

        // Act
        var result = await _controller.GetBusinessBranches(id);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo(branches));
    }

    [Test]
    public async Task GetBusinessBranches_ReturnsNotFound_WhenBusinessNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.GetBusinessBranchesAsync(id))
            .ThrowsAsync(new BusinessNotFoundException("Business not found"));

        // Act
        var result = await _controller.GetBusinessBranches(id);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteBranch_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.DeleteBranchesAsync(id))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteBranch(id);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task DeleteBranch_ReturnsNotFound_WhenBranchNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _serviceMock
            .Setup(s => s.DeleteBranchesAsync(id))
            .ThrowsAsync(new BranchNotFoundException("Branch not found"));

        // Act
        var result = await _controller.DeleteBranch(id);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UpdateBranch_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var dto = new BranchUpdateDto(
            Id: Guid.NewGuid(),
            BusinessId: Guid.NewGuid(),
            BranchName: "Main Branch",
            BranchStreet: "123 Street",
            BranchCityTown: "City",
            BranchState: "State"
        );
    
        _serviceMock
            .Setup(s => s.UpdateBranchesAsync(It.IsAny<BranchUpdateDto>()))
            .ReturnsAsync((BusinessBranches)null!);
    
        // Act
        var result = await _controller.UpdateBranch(dto);
    
        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
    
   
    [Test]
    public async Task UpdateBranch_ReturnsNotFound_WhenBranchNotFound()
    {
        // Arrange
        var dto = new BranchUpdateDto(
            Id: Guid.NewGuid(),
            BusinessId: Guid.NewGuid(),
            BranchName: "Main Branch",
            BranchStreet: "123 Street",
            BranchCityTown: "City",
            BranchState: "State");

        _serviceMock
            .Setup(s => s.UpdateBranchesAsync(dto))
            .ThrowsAsync(new BranchNotFoundException("Branch not found"));

        // Act
        var result = await _controller.UpdateBranch(dto);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

}
