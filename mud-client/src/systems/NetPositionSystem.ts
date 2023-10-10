import { Actor, Entity, Scene, System, SystemType, TransformComponent } from "excalibur";
import { NetPositionComponent } from "../components/NetPositionComponent";
import { LerpDestinationComponent } from "../components/LerpDestinationComponent";

export class NetPositionSystem extends System<NetPositionComponent> {
  // Types need to be listed as a const literal
  public readonly types = ['netPosition'] as const;

  // Run this system in the "update" phase
  public systemType = SystemType.Update


  private scene: Scene | undefined;

  override initialize(scene: Scene): void {
    this.scene = scene;
  }

  private updateActor(entity: Actor) {
    var e = entity.get<NetPositionComponent>('netPosition')!;

    if (entity.has('lerpDestination'))
      entity.removeComponent('lerpDestination');

    if (e.exists) {
      entity.z = e.position.z;

      const newX = e.position.x * 32;
      const newY = e.position.y * 32;

      const dist = Math.sqrt(Math.pow(entity.pos.x - newX, 2) + Math.pow(entity.pos.y - newY, 2))

      if (dist > 32 * 1.5) {
        entity.pos.x = e.position.x * 32;
        entity.pos.y = e.position.y * 32;
      } else {
        entity.addComponent(new LerpDestinationComponent(e.position.x * 32, e.position.y * 32, Date.now() + 150));
      }
    }

    entity.removeComponent('netPosition');
  }

  public update(entities: Entity[], delta: number) {
    for (let entity of entities) {
      if (entity instanceof Actor)
        this.updateActor(entity as Actor);/*
      var e = entity.get<NetPositionComponent>('netPosition')!;
      if (entity.has('ex.transform'))
        entity.removeComponent('ex.transform');

      if (e.exists) {
        const transform = new TransformComponent();
        transform.z = e.position.z;
        transform.pos.x = e.position.x * 32;
        transform.pos.y = e.position.y * 32;

        entity.addComponent(transform);
      }

      entity.removeComponent('netPosition');*/
    }
  }
}
