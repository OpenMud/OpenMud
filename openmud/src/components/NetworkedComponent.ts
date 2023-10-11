import { Component } from "excalibur";

export class NetworkedComponent extends Component<'networked'> {
  public readonly type = 'networked';

  constructor() {
    super();
  }
}
