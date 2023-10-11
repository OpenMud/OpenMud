import { System, Component, SystemType, Scene, Entity, GraphicsComponent, Actor, Material, Color, PostProcessor, Shader } from "excalibur";
import { NetDeletedComponent } from "../components/NetDeletedComponent";
import { NetComponent } from "../components/NetComponent";
import { NetConfigureIconComponent, NetIconComponent } from "../components/NetIconComponent";
import { SvrDirection } from "../models/SvrDirection";
import { NetDirectionComponent } from "../components/NetDirectionComponent";
import { ResolveAnimationName } from "../models/Direction";
import { ActiveAnimationComponent } from "../components/ActiveAnimationComponent";
import { NetVisibilityComponent } from "../components/NetVisibilityComponent";

const testMaterial: string = `#version 300 es
  #define LOWP lowp

  precision mediump float;
  // UV coord
  in vec2 v_uv;
  uniform sampler2D u_graphic;
  uniform vec4 u_color;
  uniform float u_opacity;
  uniform float time;
  out vec4 fragColor;
  void main() {
    vec4 color = texture(u_graphic, v_uv);
    
    float currentOpacity = texture(u_graphic, v_uv).a;

    if (currentOpacity == 1.0)
    {
        // If within the outline width, color the pixel red
        fragColor = vec4(color.r + time, color.g + time, color.b + time, color.a + time);
    }
    else
      fragColor = color;
  }
`;


export class NetIconSystem extends System<NetConfigureIconComponent> {
  // Types need to be listed as a const literal
  public readonly types = ['netConfigureIcon'] as const;

  // Run this system in the "update" phase
  public systemType = SystemType.Update

  private selectedMaterial: Material | undefined = undefined;

  private resolveIconAnimation(e: Entity): string | undefined {
    const icon = e.get<NetConfigureIconComponent>('netIcon')!;
    const dir = e.has('netDirection') ? e.get<NetDirectionComponent>('netDirection')!.direction.direction : 0;

    return ResolveAnimationName(this.gameIconAnimationsIndex, icon.icon!.icon, icon.icon!.state, dir);
  }

  public constructor(private readonly gameIcons: any, private readonly gameIconAnimationsIndex: any) { 
    super();
  }

  public updateGraphic(entity: Actor, config: NetConfigureIconComponent) {
    let workingConfig: NetIconComponent | undefined = undefined;

    if (config.isRefresh) {
      if (!entity.has('netIcon'))
        return;

      workingConfig = entity.get<NetIconComponent>('netIcon')!;
    }
    else
      workingConfig = new NetIconComponent(config.frame, true, config.icon!);

    
    entity.addComponent(workingConfig);

    const icon = workingConfig.icon!.icon;
    const fieldName = icon as keyof typeof this.gameIcons;

    if (!this.gameIcons.hasOwnProperty(fieldName))
      return;

    const animation = this.resolveIconAnimation(entity);

    if (!animation) {
      console.log("Animation not found. Skipping loading graphic");
      return;
    }

    if (entity.has('activeAnimation')) {
      const cur = entity.get<ActiveAnimationComponent>('activeAnimation')!;

      console.log("Setting animation... " + animation);
      entity.removeComponent('activeAnimation');
    }

    const gfx = this.gameIcons[fieldName].getAnimation(animation)! as ex.Animation;

    if(entity.has('interactionSelected'))
      entity.graphics.material = this.selectedMaterial!;
    else {
      //So apparently this actually can be assigned to undefined........
      let w: any = entity.graphics;
      w.material = undefined;
    }

    entity.graphics.add(gfx)
    entity.graphics.use("default");

    if(!entity.has('netVisible') || !entity.get<NetVisibilityComponent>('netVisible')!.visible)
      entity.graphics.visible = false;
    else
      entity.graphics.visible = true;

    entity.addComponent(new ActiveAnimationComponent(icon, animation)); 
  }

  public override initialize(scene: Scene): void {
    this.selectedMaterial = scene.engine.graphicsContext.createMaterial({ name: "highlighted", fragmentSource: testMaterial });
  }

  public update(entities: Entity[], delta: number) {
    this.selectedMaterial?.getShader()?.use();
    this.selectedMaterial!.getShader()?.trySetUniformFloat('time', (Date.now() % 500) / 500.0);


    for (let entity of entities) {
      if (!(entity instanceof Actor))
        continue;

      var e = entity.get<NetConfigureIconComponent>('netConfigureIcon')!;

      if (e.exists) {
        this.updateGraphic(entity, e)
      }

      entity.removeComponent('netConfigureIcon');
    }
  }
}
