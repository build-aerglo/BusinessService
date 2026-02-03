using BusinessService.Application.DTOs.AutoResponse;
using BusinessService.Application.Interfaces;
using BusinessService.Domain.Entities;
using BusinessService.Domain.Exceptions;
using BusinessService.Domain.Repositories;

namespace BusinessService.Application.Services;

public class BusinessAutoResponseService : IBusinessAutoResponseService
{
    private readonly IBusinessAutoResponseRepository _repository;
    private readonly IBusinessRepository _businessRepository;

    public BusinessAutoResponseService(
        IBusinessAutoResponseRepository repository,
        IBusinessRepository businessRepository)
    {
        _repository = repository;
        _businessRepository = businessRepository;
    }

    public async Task<BusinessAutoResponseDto?> GetByBusinessIdAsync(Guid businessId)
    {
        var autoResponse = await _repository.FindByBusinessIdAsync(businessId);
        if (autoResponse == null)
            return null;

        return MapToDto(autoResponse);
    }

    public async Task<BusinessAutoResponseDto> UpdateAsync(Guid businessId, UpdateBusinessAutoResponseRequest request)
    {
        var business = await _businessRepository.FindByIdAsync(businessId);
        if (business == null)
            throw new BusinessNotFoundException($"Business with ID {businessId} not found");

        var autoResponse = await _repository.FindByBusinessIdAsync(businessId);
        if (autoResponse == null)
            throw new BusinessNotFoundException($"Auto response settings for business {businessId} not found");

        // Update only provided fields
        if (request.PositiveResponse != null)
            autoResponse.PositiveResponse = request.PositiveResponse;

        if (request.NegativeResponse != null)
            autoResponse.NegativeResponse = request.NegativeResponse;

        if (request.NeutralResponse != null)
            autoResponse.NeutralResponse = request.NeutralResponse;

        if (request.AllowAutoResponse.HasValue)
            autoResponse.AllowAutoResponse = request.AllowAutoResponse.Value;

        await _repository.UpdateAsync(autoResponse);

        return MapToDto(autoResponse);
    }

    private static BusinessAutoResponseDto MapToDto(BusinessAutoResponse autoResponse)
    {
        return new BusinessAutoResponseDto
        {
            BusinessId = autoResponse.BusinessId,
            PositiveResponse = autoResponse.PositiveResponse,
            NegativeResponse = autoResponse.NegativeResponse,
            NeutralResponse = autoResponse.NeutralResponse,
            AllowAutoResponse = autoResponse.AllowAutoResponse
        };
    }
}
