import { Component } from 'excalibur'
import { SvrDirection } from '../models/SvrDirection';
import { NetComponent } from './NetComponent';

export class NetDirectionComponent extends NetComponent<'netDirection'> {
  public readonly type = 'netDirection';

  constructor(public readonly frame: number, public readonly exists: boolean, public readonly direction: SvrDirection) {
    super();
  }
}
