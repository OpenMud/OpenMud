import { Component } from "excalibur";

export class LerpDestinationComponent extends Component<'lerpDestination'> {
  public readonly type = 'lerpDestination';

  constructor(public readonly x: number, public readonly y: number, public readonly arrivalTime: number) {
    super();
  }
}
