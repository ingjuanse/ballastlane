using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Abstractions;

public interface ITokenGenerator
{
    string Generate(User user);
}
