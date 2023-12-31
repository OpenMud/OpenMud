﻿using OpenMud.Mudpiler.Compiler.Project.Project;
using OpenMud.Mudpiler.Core.RuntimeTypes;
using OpenMud.Mudpiler.Core.Scene;
using OpenMud.Mudpiler.Net.Core.Encoding;

namespace OpenMud.Mudpiler.Net.Core;

public class DmeProjectGameSimulationFactory : IGameSimulationFactory
{
    private readonly DmeProject project;
    private readonly IClientDispatcher dispatcher;
    private readonly IDmlFrameworkFactory frameworkFactory;
    private readonly IClientReceiver receiver;
    private readonly IWorldStateEncoderFactory worldEncoder;

    public DmeProjectGameSimulationFactory(string projectDirectory, IWorldStateEncoderFactory worldEncoder,
        IClientDispatcher dispatcher, IClientReceiver receiver, IDmlFrameworkFactory frameworkFactory)
    {
        this.worldEncoder = worldEncoder;
        this.dispatcher = dispatcher;
        this.receiver = receiver;
        project = DmeProject.Load(projectDirectory, new BaseEntityBuilder());
        this.frameworkFactory = frameworkFactory;
    }

    public IGameSimulation Create()
    {
        return new MudGameSimulation(
            project.Maps.Values.Single(),
            project.Logic,
            worldEncoder,
            dispatcher,
            receiver,
            frameworkFactory
        );
    }
}