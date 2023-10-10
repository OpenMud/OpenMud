import { System, Component, SystemType, Scene, Entity } from "excalibur";
import { NetDeletedComponent } from "../components/NetDeletedComponent";
import { NetComponent } from "../components/NetComponent";

export class NetEntityLifecycleSystem extends System<NetDeletedComponent> {
  // Types need to be listed as a const literal
  public readonly types = ['deleted'] as const;

  // Run this system in the "update" phase
  public systemType = SystemType.Update

  public update(entities: Entity[], delta: number) {
    for (let entity of entities) {
      const maxFrame = Math.max(... entity.getComponents().filter((v) => v instanceof NetComponent).map((v) => (v as NetComponent).frame));
      const deleteFrame = entity.get<NetDeletedComponent>('deleted')!.frame;

      if (deleteFrame >= maxFrame)
        entity.kill();
      else
        entity.removeComponent('deleted');
    }
  }
}
