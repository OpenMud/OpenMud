import { Component } from "excalibur";

export abstract class NetComponent<TypeName extends string = string> extends Component<TypeName> {
  public abstract readonly frame: number;
}
