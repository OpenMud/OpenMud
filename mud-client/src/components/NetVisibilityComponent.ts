import { Component } from 'excalibur'
import { SvrPosition } from '../models/SvrPosition';
import { NetComponent } from './NetComponent';

export class NetVisibilityComponent extends NetComponent<'netVisible'> {
  public readonly type = 'netVisible';

  constructor(public readonly frame: number, public readonly exists: boolean, public readonly visible: boolean) {
    super();
  }
}
