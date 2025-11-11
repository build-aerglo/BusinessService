

namespace BusinessService.Application.DTOs.Settings;



/// <summary>
/// Combined effective settings for a business rep
/// Includes both business-level and rep-level settings
/// </summary>
public record EffectiveSettingsDto(
    BusinessSettingsDto? BusinessSettings,
    BusinessRepSettingsDto RepSettings
);