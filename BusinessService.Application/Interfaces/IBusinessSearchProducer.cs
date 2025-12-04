using BusinessService.Application.DTOs;

namespace BusinessService.Application.Interfaces;

public interface IBusinessSearchProducer
{
    Task PublishBusinessCreatedAsync(BusinessDto dto);
    Task PublishBusinessUpdatedAsync(BusinessDto dto);
}