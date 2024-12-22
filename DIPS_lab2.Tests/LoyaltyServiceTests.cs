namespace LoyaltyServiceTests.Tests;

using Xunit;
using Moq;
using LoyaltyService.Controllers;
using LoyaltyService.Models.DomainModels;
using LoyaltyService.Models.Dto;
using LoyaltyService.Data;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using LoyaltyService.Mapping;
using Microsoft.AspNetCore.Mvc;
using LoyaltyService.ITokenService;

public class LoyaltyServiceTests
{
    private readonly Mock<ILoyalityRepository> mockRepository;
    private readonly LoyaltiesController controller;
    private readonly IMapper mapper;
    private readonly Mock<ITokenService> tokenService;

    public LoyaltyServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        mapper = config.CreateMapper();
        
        mockRepository = new Mock<ILoyalityRepository>();

        tokenService = new Mock<ITokenService>();

        controller = new LoyaltiesController(mockRepository.Object, mapper, tokenService.Object);
    }

    [Fact]
    public async Task DegradeLoyality_ReturnsOk()
    {
        // Arrange
        var username = "testUser";
        mockRepository.Setup(repo => repo.DegradeLoyality(username)).Returns(Task.CompletedTask);

        // Act
        var result = await controller.DegradeLoyality(username);

        // Assert
        Assert.IsType<OkResult>(result);
        mockRepository.Verify(repo => repo.DegradeLoyality(username), Times.Once);
    }
}