import { System, Component, SystemType, Scene, Entity, Actor, Observer, AddedComponent, RemovedComponent, Vector } from "excalibur";
import { NetDeletedComponent } from "../components/NetDeletedComponent";
import { NetComponent } from "../components/NetComponent";
import { LerpDestinationComponent } from "../components/LerpDestinationComponent";


export class PositionLerpingSystem extends System<LerpDestinationComponent> {
  // Types need to be listed as a const literal
  public readonly types = ['lerpDestination'] as const;

  // Run this system in the "update" phase
  public systemType = SystemType.Update

  public update(entities: Entity[], delta: number) {
    for (let entity of entities) {
      if (entity instanceof Actor) {
        this.lerp(entity as Actor, entity.get<LerpDestinationComponent>('lerpDestination')!, delta);
      }
    }
  }

  lerp(entity: Actor, comp: LerpDestinationComponent, deltaTime: number) {
    const timeToReachDestMs = comp.arrivalTime - Date.now();
    const dist = Math.sqrt(Math.pow(entity.pos.x - comp.x, 2) + Math.pow(entity.pos.y - comp.y, 2))

    if (timeToReachDestMs <= 0) {
      entity.removeComponent('lerpDestination');
      if (dist < 32 / 4)
        entity.pos.setTo(comp.x, comp.y);
      return;
    }

    const velocity = dist / (timeToReachDestMs);
    const delta = new Vector(comp.x, comp.y).sub(entity.pos).normalize().scale(velocity * deltaTime);

    entity.pos = entity.pos.add(delta);
  }
}
