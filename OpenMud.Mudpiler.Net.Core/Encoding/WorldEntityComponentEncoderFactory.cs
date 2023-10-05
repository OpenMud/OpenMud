using System.Collections.Immutable;
using DefaultEcs;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMud.Mudpiler.Net.Core.Encoding;

public class WorldEntityComponentEncoderFactory : IWorldStateEncoderFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly IImmutableList<ServiceDescriptor> services;

    public WorldEntityComponentEncoderFactory(IServiceProvider provider, IImmutableList<ServiceDescriptor> services)
    {
        serviceProvider = provider;
        this.services = services;
    }

    public IWorldStateEncoder Create(World world)
    {
        return new WorldEntityComponentEncoder(world, serviceProvider, services);
    }
}