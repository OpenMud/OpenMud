import { Actor, Entity, Keys, Scene, System, SystemType, TransformComponent, vec, Vector } from "excalibur";
import { NetPositionComponent } from "../components/NetPositionComponent";
import { NetPlayerCharacterComponent } from "../components/NetPlayerCharacterComponent";
import { SvrActionableCommands, SvrCommandDetails } from "../models/SvrActionableCommands";
import { NetActionableCommands } from "../components/NetActionableCommands";
import { InteractionSelectedComponent } from "../components/InteractionSelectedComponent";
import { NetConfigureIconComponent } from "../components/NetIconComponent";
import { DispatchMovementMessage } from "../messages/DispatchMovementMessage";

export interface ICommandInteractionWidget {
  get enableGameInput(): boolean;

  availableCommands(commands: SvrActionableCommands | undefined): void;
  activeEntity(): string | undefined;
}

class NullCommandListener implements ICommandInteractionWidget {
  get enableGameInput(): boolean {return true;}
  availableCommands(commands: SvrActionableCommands) { }
  activeEntity(): string | undefined { return undefined }
}

export class NetPlayerControlSystem extends System<NetPlayerCharacterComponent> {
  // Types need to be listed as a const literal
  public readonly types = ['playerCharacter'] as const;

  // Run this system in the "update" phase
  public systemType = SystemType.Update

  private lastPlayer: string | null = null;

  private scene: Scene | undefined;

  private actionableCommands: SvrActionableCommands | undefined;
  private lastestCommands: number = 0;
  private lastMovement: Vector = vec(0, 0);

  private lastSelection: string | undefined = undefined;

  public get playerEntity(): string | null { return this.lastPlayer };

  public constructor(private readonly listener: ICommandInteractionWidget = new NullCommandListener()) {
    super();
  }

  override initialize(scene: Scene): void {
    this.scene = scene;
  }

  private updatePlayerActor(entity: Entity) {

    if (entity instanceof Actor && this.lastPlayer != entity.name) {
      this.scene?.camera.strategy.elasticToActor(entity as Actor, .2, .8);
      this.lastPlayer = entity.name;
      this.lastestCommands = 0;
      this.actionableCommands = undefined;
      this.listener.availableCommands(undefined)
    }
  }

  private handleMovementInput(entity: Entity) {
    if(!this.listener.enableGameInput || !this.lastPlayer)
      return;

    let deltaX = this.scene?.engine.input.keyboard.isHeld(Keys.Left) ? -1 : 0;
    deltaX += this.scene?.engine.input.keyboard.isHeld(Keys.Right) ? 1 : 0;

    let deltaY = this.scene?.engine.input.keyboard.isHeld(Keys.Up) ? -1 : 0;
    deltaY += this.scene?.engine.input.keyboard.isHeld(Keys.Down) ? 1 : 0;

    const newMovement = vec(deltaX, deltaY);

    if(newMovement.distance(this.lastMovement) > 0.01) {
      this.lastMovement = newMovement;
      this.scene?.emit('dispatchMovement', new DispatchMovementMessage(this.lastPlayer, this.lastMovement));
    }
    /*
    if (deltaX == 0 && deltaY == 0) {
      if (entity.has('requestMovement')) {
        entity.removeComponent('requestMovement');
      }
    } else {
      if (entity.has('requestMovement')) {
        const currentMovement = entity.get<RequestMovementComponent>('requestMovement')!;

        if (currentMovement.x != deltaX || currentMovement.y != deltaY) {
          entity.removeComponent('requestMovement', true);
          entity.addComponent(new RequestMovementComponent(deltaX, deltaY));
        }
      } else
        entity.addComponent(new RequestMovementComponent(deltaX, deltaY));
    }*/
  }

  private removeSelected(name: string | undefined): void {
    if (!name)
      return;

    const e = this.scene?.world.entityManager.getByName(name);

    if (!e || e.length == 0)
      return;

    for (const w of e) {
      if (w.has('interactionSelected')) {
        w.removeComponent('interactionSelected');
        w.addComponent(NetConfigureIconComponent.CreateRefresh());
      }
    }
  }

  private addSelected(name: string | undefined): void {
    if (!name)
      return;

    const e = this.scene?.world.entityManager.getByName(name);

    if (!e || e.length == 0)
      return;

    this.removeSelected(name);

    for (const w of e) {
      w.addComponent(new InteractionSelectedComponent());
      w.addComponent(NetConfigureIconComponent.CreateRefresh());
    }
  }


  public update(entities: Entity[], delta: number) {
    for (let entity of entities) {
      this.updatePlayerActor(entity);
      this.handleMovementInput(entity);

      if (entity.has('actionableCommands')) {
        const cmds = entity.get<NetActionableCommands>('actionableCommands')!;
        if (cmds.frame > this.lastestCommands) {
          this.actionableCommands = cmds.commands;
          this.lastestCommands = cmds.frame;
          this.listener.availableCommands(cmds.commands);
        }
      }

      const activeSelection = this.listener.activeEntity();

      if (activeSelection != this.lastSelection) {
        this.removeSelected(this.lastSelection);
        this.addSelected(activeSelection);

        this.lastSelection = activeSelection;
      }
    }
  }
}
