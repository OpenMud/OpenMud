import { System, SystemType, Scene, Actor, Entity } from "excalibur";

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
import { NetVisibilityComponent } from "../components/NetVisibilityComponent";
import { SvrEntitySet } from "../models/SvrEntitySet";

export class NetSynchronizerSystem extends System<NetworkedComponent> {
  public readonly types = ['networked'] as const;

  // Run this system in the "update" phase
  public systemType = SystemType.Update;

  private pendingActions: Map<string, NetComponent[]> = new Map<string, NetComponent[]>

  private connectionTimeout: number = 0;

  private scene: Scene | undefined;

  constructor(private readonly hubConnection : signalR.HubConnection) {
    super();

    this.hubConnection.on('DeleteEntity', (frame: number, identifier: string) => {
      //console.log(`Delete: ${frame}, ${identifier}`);
      this.queuePending(identifier, new NetDeletedComponent(frame));
    });

    this.hubConnection.on('SetPosition', (frame: number, identifier: string, position: SvrPosition) => {
      console.log("Set Position: " + position);
      this.queuePending(identifier, new NetPositionComponent(frame, true, position));
    });

    this.hubConnection.on('SetIcon', (frame: number, identifier: string, icon: SvrIcon) => {
      //console.log(`SetIcon: ${frame}, ${identifier}, ${icon}`);
      this.queuePending(identifier, new NetConfigureIconComponent(frame, true, icon));
    });

    this.hubConnection.on('SetCommands', (frame: number, identifier: string, commands: SvrActionableCommands) => {
      //console.log(`SetCommands: ${frame}, ${identifier}, ${commands}`);
      const c = commands.commands.map(x => x.verb);
      console.log("Commands: " + commands.commands);
      this.queuePending(identifier, new NetActionableCommands(frame, true, commands));
    });

    this.hubConnection.on('SetDirection', (frame: number, identifier: string, direction: SvrDirection) => {
      //console.log(`SetCommands: ${frame}, ${identifier}, ${direction}`);
      this.queuePending(identifier, new NetDirectionComponent(frame, true, direction));

      this.queuePending(identifier, NetConfigureIconComponent.CreateRefresh());
    });

    this.hubConnection.on('SetOwnership', (frame: number, identifier: string) => {
      console.log("Set ownership of entity: " + identifier);
      this.queuePending(identifier, new NetPlayerCharacterComponent(frame));
    });

    this.hubConnection.on('SetVisible', (frame: number, player: string, visibility: any) => {
      console.log("Set entity visibility!" + player);

      for(const i of visibility.entities) {
        this.queuePending(i, new NetVisibilityComponent(frame, true, true));
        this.queuePending(i, NetConfigureIconComponent.CreateRefresh());
      }
      //this.queuePending(identifier, new NetPlayerCharacterComponent(frame));
    });

    this.hubConnection.on('SetInvisible', (frame: number, player: string, visibility: SvrEntitySet) => {
      console.log("Set entity visibility!" + player);

      for(const i of visibility.entities) {
        this.queuePending(i, new NetVisibilityComponent(frame, true, false));
        this.queuePending(i, NetConfigureIconComponent.CreateRefresh());
      }
      //this.queuePending(identifier, new NetPlayerCharacterComponent(frame));
    });
  }

  private queuePending(identifier: string, component: NetComponent<string>) {
    if (!this.pendingActions.has(identifier))
      this.pendingActions.set(identifier, []);

    const r = this.pendingActions.get(identifier)!;
    r.push(component);
    this.pendingActions.set(identifier, r);
  }

  override initialize(scene: Scene): void {
    this.scene = scene;
  }

  public get isConnected(): boolean {
    return this.hubConnection.state == signalR.HubConnectionState.Connected;
  }

  private connect() {
    this.hubConnection.start().catch((error) => {
      console.error('Error starting SignalR connection:', error);
    });

    this.connectionTimeout = 10000;
  }

  private updateDisconnected(entities: Entity[], delta: number) {
    this.connectionTimeout = Math.max(0, this.connectionTimeout - delta);
    entities.forEach((e) => e.kill());
    this.pendingActions.clear();

    if (this.connectionTimeout === 0)
      this.connect();
  }

  private createActor(name: string): Actor {
    const entity = new Actor({
      name: name
    });

    entity.addComponent(new NetworkedComponent());

    return entity;
  }

  private processCommands(entities: ex.Entity[]) {

    const entityMapping = new Map<string, ex.Entity>()
    entities.forEach((e) => entityMapping.set(e.name, e));

    this.pendingActions.forEach((v, k) => {
      if (!entityMapping.has(k)) {
        const entity = this.createActor(k);

        this.scene?.add(entity);
        entityMapping.set(k, entity);
      }

      const entity = entityMapping.get(k)!;

      for (const c of v) {
        if (entity.has(c.type)) {
          if (c.frame < (entity.get(c.type)! as NetComponent).frame)
            continue;
          else
            entity.removeComponent(c.type, true);
        }

        entity.addComponent(c);
      }
    });

    this.pendingActions.clear();
  }

  public update(entities: Entity[], delta: number) {
    if (!this.isConnected)
      this.updateDisconnected(entities, delta);
    else
      this.processCommands(entities);
  }
}
