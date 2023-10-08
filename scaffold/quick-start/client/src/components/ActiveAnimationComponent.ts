import { Component } from "excalibur";

export class ActiveAnimationComponent extends Component<'activeAnimation'> {
  public readonly type = 'activeAnimation';

  constructor(public readonly icon: string, public readonly animation: string) {
    super();
  }
}
