using Mapster;

namespace Enterprise.Application.Common.Mappings;

/// <summary>
/// Mapster over AutoMapper: compile-time-friendly (mappings can be compiled via
/// <c>TypeAdapterConfig.Compile()</c> for near-zero-allocation mapping), no runtime profile
/// resolution magic, and simple conventions cover ~95% of this template's DTOs without any
/// entry here at all.
///
/// Product is mapped via <c>ProductMapping.ToDto</c> so name/description/category resolve
/// against the request culture. Entities are intentionally mapped ONE WAY, entity -&gt; DTO.
/// </summary>
public sealed class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
    }
}
