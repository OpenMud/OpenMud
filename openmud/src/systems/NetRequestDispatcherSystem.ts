import { System, Component, SystemType, Scene, Entity, Actor, AddedComponent, Observer, RemovedComponent, SceneEvents } from "excalibur";

import * as signalR from '@microsoft/signalr';
import { NetworkedComponent } from "../components/NetworkedComponent";
import { NetComponent } from "../components/NetComponent";
import { SvrIcon } from "../models/SvrIcon";
import { SvrPosition } from "../models/SvrPosition";
import { SvrActionableCommands } from "../models/SvrActionableCommands";
import { NetPositionComponent } from "../components/NetPositionComponent";
import { NetDeletedComponent } from "../components/NetDeletedComponent";
import { NetConfigureIconComponent } from "../components/NetIconComponent";
import { SvrDirection } from "../models/SvrDirection";
import { NetDirectionComponent } from "../components/NetDirectionComponent";
import { NetPlayerCharacterComponent } from "../components/NetPlayerCharacterComponent";
import { NetActionableCommands } from "../components/NetActionableCommands";
import { DispatchVerbMessage } from "../messages/DispatchVerbMessage";
import { DispatchMovementMessage } from "../messages/DispatchMovementMessage";

export class NetRequestDispatcherSystem extends System {
  public systemType = SystemType.Update
  public readonly types = ['untyped_system'] as const;

  private scene: Scene | undefined;

  constructor(private readonly hub : signalR.HubConnection) {
    super();
  }

  override initialize(scene: Scene): void {
    this.scene = scene;
    this.scene.on('dispatchVerb', (t) => this.DispatchVerb(t as DispatchVerbMessage))
    this.scene.on('dispatchMovement', (t) => this.DispatchMovement(t as DispatchMovementMessage));

    console.log("Inited!");
  }

  DispatchVerb(cmd: DispatchVerbMessage): void {
    this.hub.send("ExecuteVerb", cmd.source, cmd.target, cmd.command);
  }
  
  DispatchMovement(cmd: DispatchMovementMessage): void {
    if (cmd.movement.size >= 0.1)
      this.hub.send("RequestMovement", cmd.entity, cmd.movement.x, cmd.movement.y);
    else
      this.hub.send("ClearMovementRequest", cmd.entity);
  }

  public update(entities: Entity[], delta: number) { }

}

