import { Component } from 'excalibur'
import { SvrIcon } from '../models/SvrIcon';
import { NetComponent } from './NetComponent';

export class NetConfigureIconComponent extends NetComponent<'netConfigureIcon'> {
  public readonly type = 'netConfigureIcon';
  public readonly isRefresh: boolean;

  constructor(public readonly frame: number, public readonly exists: boolean, public readonly icon?: SvrIcon) {
    super();
    this.isRefresh = icon == null;
  }

  static CreateRefresh(): NetConfigureIconComponent {
    return new NetConfigureIconComponent(0, true, undefined);
  }
}


export class NetIconComponent extends NetComponent<'netIcon'> {
  public readonly type = 'netIcon';

  constructor(public readonly frame: number, public readonly exists: boolean, public readonly icon: SvrIcon) {
    super();
  }
}
