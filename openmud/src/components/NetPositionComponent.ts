import { Component } from 'excalibur'
import { SvrPosition } from '../models/SvrPosition';
import { NetComponent } from './NetComponent';

export class NetPositionComponent extends NetComponent<'netPosition'> {
  public readonly type = 'netPosition';

  constructor(public readonly frame: number, public readonly exists: boolean, public readonly position: SvrPosition) {
    super();
  }
}
