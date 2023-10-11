import { Color, DisplayMode, Engine, EventKey, GameEvent, KeyEvent, PointerScope, Loader } from 'excalibur'
import { NetSynchronizerSystem } from './systems/NetSynchronizerSystem';
import { NetEntityLifecycleSystem } from './systems/NetEntityLifecycleSystem';
import { NetIconSystem } from './systems/NetIconSystem';
import { NetPositionSystem } from './systems/NetPositionSystem';
import { ICommandInteractionWidget, NetPlayerControlSystem } from './systems/NetPlayerControlSystem';
import { PositionLerpingSystem } from './systems/PositionLerpingSystem';
import { SvrActionableCommands } from './models/SvrActionableCommands';
import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import { DispatchVerbMessage } from './messages/DispatchVerbMessage';
import { NetRequestDispatcherSystem } from './systems/NetRequestDispatcherSystem';
import { HubConnectionBuilder, HubConnection, JsonHubProtocol } from '@microsoft/signalr';

export class MudGame extends Engine {
  private playerControlSystem: NetPlayerControlSystem | undefined = undefined;

  constructor(canvas: string, private readonly resourceLoader: Loader, private readonly gameIcons: any, private readonly gameIconAnimationsIndex: any) {
    super({
      displayMode: DisplayMode.FillScreen,
      canvasElementId: canvas,
      backgroundColor: new Color(0, 0, 0),
      pointerScope: PointerScope.Canvas
    });
  }

  createConnection() : HubConnection {
    return new HubConnectionBuilder()
      .withUrl('https://localhost:7087/worldHub')
      .withHubProtocol(new JsonHubProtocol())
      .build();
  }

  initialize(interaction: ICommandInteractionWidget) {
    const connection = this.createConnection();

    this.start(this.resourceLoader);

    this.playerControlSystem = new NetPlayerControlSystem(interaction);

    this.currentScene.world.add(new NetSynchronizerSystem(connection));
    this.currentScene.world.add(new NetEntityLifecycleSystem());
    this.currentScene.world.add(new NetIconSystem(this.gameIcons, this.gameIconAnimationsIndex));
    this.currentScene.world.add(new NetPositionSystem());
    this.currentScene.world.add(this.playerControlSystem!);
    this.currentScene.world.add(new PositionLerpingSystem());
    this.currentScene.world.add(new NetRequestDispatcherSystem(connection));
  }

  dispatchCommand(entity: string | undefined, commandString: string) {
    const source = this.playerControlSystem!.playerEntity;
    
    if(!source)
      return;

    const target = entity;
    this.currentScene.emit('dispatchVerb', new DispatchVerbMessage(source, target, commandString));
  }
}