import { Color, DisplayMode, Engine, EventKey, GameEvent, KeyEvent, PointerScope, Loader } from 'excalibur'
import { NetworkSystem } from './systems/NetSynchronizerSystem';
import { NetEntityLifecycleSystem } from './systems/NetEntityLifecycleSystem';
import { NetIconSystem } from './systems/NetIconSystem';
import { GameResourceLoader } from './resources';
import { NetPoitionSystem } from './systems/NetPositionSystem';
import { ICommandInteractionWidget, NetPlayerControlSystem } from './systems/NetPlayerControlSystem';
import { PositionLerpingSystem } from './systems/PositionLerpingSystem';
import { SvrActionableCommands } from './models/SvrActionableCommands';
import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import { DispatchVerbMessage } from './messages/DispatchVerbMessage';
import { NetRequestDispatcherSystem } from './systems/NetRequestDispatcherSystem';
import { HubConnectionBuilder, HubConnection, JsonHubProtocol } from '@microsoft/signalr';

export class Game extends Engine {
  private playerControlSystem: NetPlayerControlSystem | undefined = undefined;

  constructor(canvas: string) {
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

    this.start(GameResourceLoader);

    this.playerControlSystem = new NetPlayerControlSystem(interaction);

    this.currentScene.world.add(new NetworkSystem(connection));
    this.currentScene.world.add(new NetEntityLifecycleSystem());
    this.currentScene.world.add(new NetIconSystem());
    this.currentScene.world.add(new NetPoitionSystem());
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