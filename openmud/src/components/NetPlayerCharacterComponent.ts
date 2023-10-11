import { Component } from 'excalibur'
import { SvrPosition } from '../models/SvrPosition';
import { NetComponent } from './NetComponent';

export class NetPlayerCharacterComponent extends NetComponent<'playerCharacter'> {
  public readonly type = 'playerCharacter';

  constructor(public readonly frame: number) {
    super();
  }
}
